using Microsoft.EntityFrameworkCore;
using JobAutoApplyNET.Data;
using JobAutoApplyNET.Models;
using JobAutoApplyNET.Services;

namespace JobAutoApplyNET.Endpoints;

public static class JobEndpoints
{
    public static RouteGroupBuilder MapJobEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/jobs")
                          .WithTags("Job Operations");

        // GET /api/jobs
        group.MapGet("/", async (string? country, string? status, int? minScore, JobDbContext db) =>
        {
            var query = db.JobPostings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(j => j.Country.ToLower().Contains(country.ToLower()) || 
                                         j.Location.ToLower().Contains(country.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(j => j.Status == status);
            }

            if (minScore.HasValue)
            {
                query = query.Where(j => j.MatchScore >= minScore.Value);
            }

            var jobs = await query
                .OrderByDescending(j => j.MatchScore)
                .ThenByDescending(j => j.DiscoveredAt)
                .Take(200)
                .ToListAsync();

            return Results.Ok(jobs);
        })
        .WithName("GetJobs")
        .WithSummary("Retrieve matching job postings with filters for country, status, and score");

        // GET /api/jobs/{id}
        group.MapGet("/{id:int}", async (int id, JobDbContext db) =>
        {
            var job = await db.JobPostings.FindAsync(id);
            return job is not null ? Results.Ok(job) : Results.NotFound();
        })
        .WithName("GetJobById");

        // POST /api/jobs/scan
        group.MapPost("/scan", async (
            AggregatorJobService aggregator,
            AppConfig config,
            UserProfile profile,
            bool autoApply = false) =>
        {
            var result = await aggregator.ScanAndProcessJobsAsync(config, profile, autoApply);
            return Results.Ok(result);
        })
        .WithName("TriggerJobScan")
        .WithSummary("Trigger instant multi-source job discovery across Dubai, Germany & Saudi Arabia");

        // POST /api/jobs/{id}/apply
        group.MapPost("/{id:int}/apply", async (
            int id, 
            JobDbContext db, 
            EmailApplicationService emailService,
            CoverLetterGeneratorService coverLetterService,
            UserProfile profile,
            AppConfig config) =>
        {
            var job = await db.JobPostings.FindAsync(id);
            if (job is null) return Results.NotFound();

            // Strict Blacklist Employer Guard
            if (config.IsBlacklisted(job.Company, job.JobUrl, job.ApplyEmail))
            {
                job.Status = "Blacklisted";
                job.Notes = "Excluded: Matches blacklisted employer filter.";
                await db.SaveChangesAsync();
                return Results.BadRequest(new { success = false, message = $"BLOCKED: {job.Company} is on your blacklisted employers list." });
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
        })
        .WithName("ApplyToJob");

        return group;
    }
}
