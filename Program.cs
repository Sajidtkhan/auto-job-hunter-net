using Microsoft.EntityFrameworkCore;
using JobAutoApplyNET.Data;
using JobAutoApplyNET.Models;
using JobAutoApplyNET.Services;
using JobAutoApplyNET.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Explicitly bind to assigned port (local 5000 or Cloud Provider PORT environment variable)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// JSON Case-Insensitive Serializer Configuration
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Configure SQLite Database with Connection Pooling
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

// Allow overriding SMTP Password via Environment Variable (e.g. GitHub Actions secret or Cloud Host)
var envSmtpPass = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
if (!string.IsNullOrWhiteSpace(envSmtpPass))
{
    appConfig.Smtp.Password = envSmtpPass;
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

builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(userProfile);

// Register Core Domain & Infrastructure Services (Clean Architecture)
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IJobSource, ArbeitnowJobSource>();
builder.Services.AddSingleton<IJobSource, DubaiTechJobSource>();
builder.Services.AddSingleton<JobMatchingService>();
builder.Services.AddSingleton<CoverLetterGeneratorService>();
builder.Services.AddSingleton<EmailApplicationService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<AggregatorJobService>();

// Register Daily Scheduled Background Service
builder.Services.AddHostedService<DailyJobSchedulerService>();

var app = builder.Build();

// Ensure Database is initialized and clean of any blacklisted employers
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
    db.Database.EnsureCreated();

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

// Direct CLI / Cloud Runner scan mode (for GitHub Actions automated daily 9 AM execution)
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

// ==================== CLEAN ARCHITECTURE ENDPOINT GROUPS ====================
app.MapJobEndpoints();
app.MapProfileEndpoints();
app.MapConfigAndMetricsEndpoints();

app.Run();
