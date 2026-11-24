using Microsoft.EntityFrameworkCore;
using JobAutoApplyNET.Data;
using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Services;

public class AggregatorJobService
{
    private readonly IEnumerable<IJobSource> _jobSources;
    private readonly IServiceProvider _serviceProvider;
    private readonly JobMatchingService _matchingService;
    private readonly CoverLetterGeneratorService _coverLetterService;
    private readonly EmailApplicationService _emailService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<AggregatorJobService> _logger;

    public AggregatorJobService(
        IEnumerable<IJobSource> jobSources,
        IServiceProvider serviceProvider,
        JobMatchingService matchingService,
        CoverLetterGeneratorService coverLetterService,
        EmailApplicationService emailService,
        NotificationService notificationService,
        ILogger<AggregatorJobService> logger)
    {
        _jobSources = jobSources;
        _serviceProvider = serviceProvider;
        _matchingService = matchingService;
        _coverLetterService = coverLetterService;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ScanSummaryResult> ScanAndProcessJobsAsync(
        AppConfig config, 
        UserProfile profile, 
        bool triggerAutoApply = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Automated Job Discovery for Dubai and Germany (.NET & SQL)...");
        var result = new ScanSummaryResult();

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();

        var rawJobs = new List<JobPosting>();
        foreach (var source in _jobSources)
        {
            try
            {
                var fetched = await source.FetchJobsAsync(cancellationToken);
                rawJobs.AddRange(fetched);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch from job source {SourceName}", source.SourceName);
            }
        }

        result.TotalDiscovered = rawJobs.Count;
        _logger.LogInformation("Raw job count collected across all sources: {Count}", rawJobs.Count);

        var newMatchesToSave = new List<JobPosting>();
        var existingUrls = await db.JobPostings.Select(j => j.JobUrl).ToListAsync(cancellationToken);
        var existingTitlesCompanies = await db.JobPostings
            .Select(j => j.Title.ToLower() + "|" + j.Company.ToLower())
            .ToListAsync(cancellationToken);

        foreach (var job in rawJobs)
        {
            // 0. Air-tight Blacklist Employer Guard
            if (config.IsBlacklisted(job.Company, job.JobUrl, job.ApplyEmail))
            {
                _logger.LogWarning("SECURITY SHIELD: Dropping job from blacklisted company: {Company}", job.Company);
                continue;
            }

            if (string.IsNullOrWhiteSpace(job.JobUrl) || existingUrls.Contains(job.JobUrl))
            {
                continue;
            }

            var key = job.Title.ToLower() + "|" + job.Company.ToLower();
            if (existingTitlesCompanies.Contains(key))
            {
                continue;
            }

            // Evaluate relevance for .NET, SQL, Dubai, Germany
            var (score, matchedKw, country, detectedEmail) = _matchingService.EvaluateJob(
                job.Title, job.Company, job.Location, job.Description);

            // Filter out unrelated jobs
            if (score < 40)
            {
                continue;
            }

            job.MatchScore = score;
            job.MatchedKeywords = matchedKw;
            job.Country = country;
            if (string.IsNullOrEmpty(job.ApplyEmail) && !string.IsNullOrEmpty(detectedEmail))
            {
                job.ApplyEmail = detectedEmail;
            }

            // Generate tailored cover letter
            job.GeneratedCoverLetter = _coverLetterService.GenerateCoverLetter(job, profile);

            newMatchesToSave.Add(job);
            existingUrls.Add(job.JobUrl);
            existingTitlesCompanies.Add(key);
        }

        _logger.LogInformation("Filtered {Count} high-relevance .NET & SQL jobs for Dubai and Germany.", newMatchesToSave.Count);

        // Save to Database
        if (newMatchesToSave.Count > 0)
        {
            await db.JobPostings.AddRangeAsync(newMatchesToSave, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        // Process Applications if enabled
        int appliedCount = 0;
        foreach (var job in newMatchesToSave)
        {
            bool shouldAutoApply = triggerAutoApply || config.AutoSendEmailApplication;
            if (shouldAutoApply && !string.IsNullOrWhiteSpace(job.ApplyEmail))
            {
                var (success, msg) = await _emailService.SendApplicationEmailAsync(
                    job, profile, config, job.GeneratedCoverLetter ?? "");

                if (success)
                {
                    job.Status = "Applied";
                    job.AppliedAt = DateTime.UtcNow;
                    job.Notes = msg;

                    var application = new JobApplication
                    {
                        JobPostingId = job.Id,
                        AppliedAt = DateTime.UtcNow,
                        Method = "Email",
                        Status = "Success",
                        Notes = msg
                    };
                    await db.JobApplications.AddAsync(application, cancellationToken);
                    appliedCount++;
                }
            }
        }

        if (appliedCount > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        result.NewMatchesFound = newMatchesToSave.Count;
        result.DubaiCount = newMatchesToSave.Count(j => j.Country.Contains("Dubai") || j.Country.Contains("UAE"));
        result.GermanyCount = newMatchesToSave.Count(j => j.Country.Contains("Germany"));
        result.AutoAppliedCount = appliedCount;

        // Dispatch notifications
        await _notificationService.NotifyNewJobsDiscoveredAsync(newMatchesToSave, config);

        return result;
    }
}

public class ScanSummaryResult
{
    public int TotalDiscovered { get; set; }
    public int NewMatchesFound { get; set; }
    public int DubaiCount { get; set; }
    public int GermanyCount { get; set; }
    public int AutoAppliedCount { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
