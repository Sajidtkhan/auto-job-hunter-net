using Microsoft.EntityFrameworkCore;
using JobAutoApplyNET.Data;
using JobAutoApplyNET.Models;
using JobAutoApplyNET.Services;

var builder = WebApplication.CreateBuilder(args);

// Explicitly bind to assigned port (local 5000 or Cloud Provider PORT environment variable)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// Configure SQLite Database
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "jobs.db");
builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Register AppConfig and UserProfile as persistent singletons with disk backing
var profileJsonPath = Path.Combine(builder.Environment.ContentRootPath, "profile.json");
var configJsonPath = Path.Combine(builder.Environment.ContentRootPath, "config.json");

var appConfig = new AppConfig();
if (File.Exists(configJsonPath))
{
    try 
    { 
        appConfig = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configJsonPath)) ?? appConfig; 
    } 
    catch { }
}

var userProfile = new UserProfile();
if (File.Exists(profileJsonPath))
{
    try 
    { 
        userProfile = System.Text.Json.JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(profileJsonPath)) ?? userProfile; 
    } 
    catch { }
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(userProfile);

// Register HTTP Client & Core Services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IJobSource, ArbeitnowJobSource>();
builder.Services.AddSingleton<IJobSource, DubaiTechJobSource>();
builder.Services.AddSingleton<JobMatchingService>();
builder.Services.AddSingleton<CoverLetterGeneratorService>();
builder.Services.AddSingleton<EmailApplicationService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<AggregatorJobService>();

// Register Daily Scheduled Worker Service
builder.Services.AddHostedService<DailyJobSchedulerService>();

var app = builder.Build();

// Ensure Database is created and sanitized
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
    db.Database.EnsureCreated();

    // Sanitize any blacklisted companies immediately from existing records
    var blacklisted = db.JobPostings
        .AsEnumerable()
        .Where(j => appConfig.IsBlacklisted(j.Company, j.JobUrl, j.ApplyEmail))
        .ToList();

    if (blacklisted.Count > 0)
    {
        db.JobPostings.RemoveRange(blacklisted);
        db.SaveChanges();
    }
}

// Support direct CLI / Cloud Runner scan mode (for GitHub Actions or cloud crons)
if (args.Contains("--scan-now") || args.Contains("--scan-and-apply"))
{
    bool autoApply = args.Contains("--scan-and-apply") || appConfig.AutoSendEmailApplication;
    Console.WriteLine($"[Cloud Runner] Executing automated daily job search for Dubai, Germany & Saudi Arabia (AutoApply: {autoApply})...");
    using var scope2 = app.Services.CreateScope();
    var aggregator = scope2.ServiceProvider.GetRequiredService<AggregatorJobService>();
    var res = await aggregator.ScanAndProcessJobsAsync(appConfig, userProfile, autoApply);
    Console.WriteLine($"[Cloud Runner] Finished! Found {res.NewMatchesFound} matches (Dubai: {res.DubaiCount}, Germany: {res.GermanyCount}). Auto-applied: {res.AutoAppliedCount}.");
    return;
}

app.UseDefaultFiles();
app.UseStaticFiles();

// ==================== REST API ENDPOINTS ====================

// 1. Dashboard Stats
app.MapGet("/api/stats", async (JobDbContext db) =>
{
    var total = await db.JobPostings.CountAsync();
    var dubaiCount = await db.JobPostings.CountAsync(j => j.Country.Contains("Dubai") || j.Country.Contains("UAE"));
    var germanyCount = await db.JobPostings.CountAsync(j => j.Country.Contains("Germany"));
    var appliedCount = await db.JobPostings.CountAsync(j => j.Status == "Applied");
    var highMatchCount = await db.JobPostings.CountAsync(j => j.MatchScore >= 70);

    return Results.Ok(new
    {
        totalJobs = total,
        dubaiJobs = dubaiCount,
        germanyJobs = germanyCount,
        appliedJobs = appliedCount,
        highMatchJobs = highMatchCount,
        lastUpdated = DateTime.UtcNow
    });
});

// 2. Get Jobs list with filters
app.MapGet("/api/jobs", async (
    JobDbContext db, 
    string? country, 
    string? status, 
    int? minScore) =>
{
    var query = db.JobPostings.AsQueryable();

    if (!string.IsNullOrWhiteSpace(country))
    {
        if (country.Equals("dubai", StringComparison.OrdinalIgnoreCase))
            query = query.Where(j => j.Country.Contains("Dubai") || j.Country.Contains("UAE"));
        else if (country.Equals("germany", StringComparison.OrdinalIgnoreCase))
            query = query.Where(j => j.Country.Contains("Germany"));
    }

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(j => j.Status == status);
    }

    if (minScore.HasValue)
    {
        query = query.Where(j => j.MatchScore >= minScore.Value);
    }

    var list = await query
        .OrderByDescending(j => j.MatchScore)
        .ThenByDescending(j => j.DiscoveredAt)
        .Take(100)
        .ToListAsync();

    return Results.Ok(list);
});

// 3. Get Single Job Details
app.MapGet("/api/jobs/{id:int}", async (int id, JobDbContext db) =>
{
    var job = await db.JobPostings.FindAsync(id);
    return job is not null ? Results.Ok(job) : Results.NotFound();
});

// 4. Trigger Instant Scan & Apply
app.MapPost("/api/jobs/scan", async (
    AggregatorJobService aggregator,
    AppConfig config,
    UserProfile profile,
    bool autoApply = false) =>
{
    var result = await aggregator.ScanAndProcessJobsAsync(config, profile, autoApply);
    return Results.Ok(result);
});

// 5. Apply to a specific job (via Email or Web)
app.MapPost("/api/jobs/{id:int}/apply", async (
    int id, 
    JobDbContext db, 
    EmailApplicationService emailService,
    CoverLetterGeneratorService coverLetterService,
    UserProfile profile,
    AppConfig config) =>
{
    var job = await db.JobPostings.FindAsync(id);
    if (job is null) return Results.NotFound();

    // Blacklist Guard
    if (config.IsBlacklisted(job.Company, job.JobUrl, job.ApplyEmail))
    {
        job.Status = "Blacklisted";
        job.Notes = "Excluded: Matches blacklisted employer filter.";
        await db.SaveChangesAsync();
        return Results.BadRequest(new { success = false, message = $"BLOCKED: {job.Company} is on your blacklisted employers list. Application strictly denied." });
    }

    string letter = job.GeneratedCoverLetter ?? coverLetterService.GenerateCoverLetter(job, profile);
    var (success, msg) = await emailService.SendApplicationEmailAsync(job, profile, config, letter);

    job.Status = "Applied";
    job.AppliedAt = DateTime.UtcNow;
    job.Notes = msg;

    var appLog = new JobApplication
    {
        JobPostingId = job.Id,
        AppliedAt = DateTime.UtcNow,
        Method = !string.IsNullOrEmpty(job.ApplyEmail) ? "Email" : "WebClick",
        Status = success ? "Success" : "Pending",
        Notes = msg
    };
    await db.JobApplications.AddAsync(appLog);
    await db.SaveChangesAsync();

    return Results.Ok(new { success, message = msg, job });
});

// 5b. Apply to all unapplied matches
app.MapPost("/api/jobs/apply-all", async (
    JobDbContext db,
    EmailApplicationService emailService,
    CoverLetterGeneratorService coverLetterService,
    UserProfile profile,
    AppConfig config) =>
{
    var unapplied = await db.JobPostings
        .Where(j => j.Status == "Discovered" && j.MatchScore >= 60)
        .ToListAsync();

    int applied = 0;
    var appliedList = new List<object>();

    foreach (var job in unapplied)
    {
        // Blacklist Guard
        if (config.IsBlacklisted(job.Company, job.JobUrl, job.ApplyEmail))
        {
            job.Status = "Blacklisted";
            job.Notes = "Excluded: Matches blacklisted employer filter (RevClerx, MaxVal, Mindmatrix, Arerhos).";
            continue;
        }

        string letter = job.GeneratedCoverLetter ?? coverLetterService.GenerateCoverLetter(job, profile);
        var (success, msg) = await emailService.SendApplicationEmailAsync(job, profile, config, letter);
        job.Status = "Applied";
        job.AppliedAt = DateTime.UtcNow;
        job.Notes = msg;

        var appLog = new JobApplication
        {
            JobPostingId = job.Id,
            AppliedAt = DateTime.UtcNow,
            Method = !string.IsNullOrEmpty(job.ApplyEmail) ? "Email" : "WebClick",
            Status = "Success",
            Notes = msg
        };
        await db.JobApplications.AddAsync(appLog);
        applied++;
        appliedList.Add(new { id = job.Id, title = job.Title, company = job.Company, country = job.Country, matchScore = job.MatchScore, notes = msg });
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { count = applied, jobs = appliedList });
});

// 6. Update Job Status manually
app.MapPost("/api/jobs/{id:int}/status", async (int id, string newStatus, JobDbContext db) =>
{
    var job = await db.JobPostings.FindAsync(id);
    if (job is null) return Results.NotFound();

    job.Status = newStatus;
    if (newStatus == "Applied" && !job.AppliedAt.HasValue)
    {
        job.AppliedAt = DateTime.UtcNow;
    }
    await db.SaveChangesAsync();
    return Results.Ok(job);
});

// 7. Get and Update Profile (Disk-persisted)
app.MapGet("/api/profile", (UserProfile profile) => Results.Ok(profile));
app.MapPost("/api/profile", (UserProfile updated, UserProfile profile, IWebHostEnvironment env) =>
{
    if (!string.IsNullOrWhiteSpace(updated.FullName)) profile.FullName = updated.FullName;
    if (!string.IsNullOrWhiteSpace(updated.Email)) profile.Email = updated.Email;
    if (!string.IsNullOrWhiteSpace(updated.Phone)) profile.Phone = updated.Phone;

    if (!string.IsNullOrWhiteSpace(updated.LinkedInUrl)) profile.LinkedInUrl = updated.LinkedInUrl;

    if (!string.IsNullOrWhiteSpace(updated.CurrentTitle)) profile.CurrentTitle = updated.CurrentTitle;
    if (updated.YearsOfExperience > 0) profile.YearsOfExperience = updated.YearsOfExperience;
    if (!string.IsNullOrWhiteSpace(updated.NoticePeriod)) profile.NoticePeriod = updated.NoticePeriod;
    if (!string.IsNullOrWhiteSpace(updated.TargetLocations)) profile.TargetLocations = updated.TargetLocations;
    if (!string.IsNullOrWhiteSpace(updated.SkillsSummary)) profile.SkillsSummary = updated.SkillsSummary;
    if (!string.IsNullOrWhiteSpace(updated.ResumePath)) profile.ResumePath = updated.ResumePath;

    // Persist permanently to disk
    var path = Path.Combine(env.ContentRootPath, "profile.json");
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(profile, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

    return Results.Ok(profile);
});

// 8. Get and Update Config (Disk-persisted)
app.MapGet("/api/config", (AppConfig config) => Results.Ok(config));
app.MapPost("/api/config", (AppConfig updated, AppConfig config, IWebHostEnvironment env) =>
{
    config.DailyRunHour = updated.DailyRunHour;
    config.MinMatchScoreToNotify = updated.MinMatchScoreToNotify;
    config.AutoSendEmailApplication = updated.AutoSendEmailApplication;
    if (updated.BlacklistedCompanies != null && updated.BlacklistedCompanies.Count > 0)
    {
        config.BlacklistedCompanies = updated.BlacklistedCompanies;
    }
    config.Smtp = updated.Smtp;
    config.Telegram = updated.Telegram;

    // Persist permanently to disk
    var path = Path.Combine(env.ContentRootPath, "config.json");
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

    return Results.Ok(config);
});

app.Run();
