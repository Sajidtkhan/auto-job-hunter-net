using Microsoft.EntityFrameworkCore;
using JobAutoApplyNET.Data;
using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigAndMetricsEndpoints(this IEndpointRouteBuilder routes)
    {
        // Stats
        routes.MapGet("/api/stats", async (JobDbContext db) =>
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
        })
        .WithTags("Analytics")
        .WithSummary("Retrieve real-time job application metrics and region counts");

        // Config
        var configGroup = routes.MapGroup("/api/config").WithTags("Automation Configuration");

        configGroup.MapGet("/", (AppConfig config) => Results.Ok(config))
                   .WithName("GetConfig");

        configGroup.MapPost("/", (AppConfig updated, AppConfig config, IWebHostEnvironment env) =>
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
        })
        .WithName("UpdateConfig")
        .WithSummary("Update daily scheduled hour, strict employer blacklist, and SMTP credentials");
    }
}
