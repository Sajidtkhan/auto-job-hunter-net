using System.Text.Json;
using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Services;

public class NotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(HttpClient httpClient, ILogger<NotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task NotifyNewJobsDiscoveredAsync(List<JobPosting> newJobs, AppConfig config)
    {
        if (newJobs.Count == 0) return;

        var dubaiCount = newJobs.Count(j => j.Country.Contains("Dubai") || j.Country.Contains("UAE"));
        var germanyCount = newJobs.Count(j => j.Country.Contains("Germany"));

        var summary = $"Daily Job Agent Alert: Found {newJobs.Count} new .NET & SQL opportunities! " +
                      $"(Dubai: {dubaiCount}, Germany: {germanyCount})";

        _logger.LogInformation("==================================================");
        _logger.LogInformation("{Summary}", summary);
        foreach (var job in newJobs.OrderByDescending(j => j.MatchScore).Take(5))
        {
            _logger.LogInformation("-> [{Country}] {Title} @ {Company} (Match: {Score}%) | URL: {Url}",
                job.Country, job.Title, job.Company, job.MatchScore, job.JobUrl);
        }
        _logger.LogInformation("==================================================");

        // Send to Telegram if configured
        if (config.Telegram.Enabled && !string.IsNullOrWhiteSpace(config.Telegram.BotToken) && !string.IsNullOrWhiteSpace(config.Telegram.ChatId))
        {
            try
            {
                var telegramMsg = $"🤖 *Daily .NET & SQL Job Update*\n\n" +
                                  $"📍 *Dubai & Germany Focus*\n" +
                                  $"✨ Found *{newJobs.Count}* new matches (Dubai: {dubaiCount}, Germany: {germanyCount})\n\n";

                foreach (var job in newJobs.OrderByDescending(j => j.MatchScore).Take(5))
                {
                    telegramMsg += $"• *{job.Title}* at *{job.Company}*\n" +
                                   $"  📍 {job.Location} (Match: {job.MatchScore}%)\n" +
                                   $"  🔗 [View & Apply]({job.JobUrl})\n\n";
                }

                var url = $"https://api.telegram.org/bot{config.Telegram.BotToken}/sendMessage";
                var payload = new
                {
                    chat_id = config.Telegram.ChatId,
                    text = telegramMsg,
                    parse_mode = "Markdown",
                    disable_web_page_preview = true
                };

                await _httpClient.PostAsJsonAsync(url, payload);
                _logger.LogInformation("Telegram notification sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Telegram notification");
            }
        }
    }
}
