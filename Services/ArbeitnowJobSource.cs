using System.Text.Json;
using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Services;

public class ArbeitnowJobSource : IJobSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArbeitnowJobSource> _logger;

    public string SourceName => "Arbeitnow Germany";

    public ArbeitnowJobSource(HttpClient httpClient, ILogger<ArbeitnowJobSource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<JobPosting>> FetchJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = new List<JobPosting>();
        try
        {
            _logger.LogInformation("Fetching jobs from Arbeitnow Germany API...");
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.arbeitnow.com/api/job-board-api");
            request.Headers.Add("User-Agent", "JobAutoApplyNET/1.0 (Windows NT 10.0; Win64; x64)");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Arbeitnow API returned status code: {StatusCode}", response.StatusCode);
                return jobs;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    var title = item.GetProperty("title").GetString() ?? "";
                    var company = item.GetProperty("company_name").GetString() ?? "";
                    var location = item.GetProperty("location").GetString() ?? "Germany";
                    var url = item.GetProperty("url").GetString() ?? "";
                    var description = item.GetProperty("description").GetString() ?? "";

                    var job = new JobPosting
                    {
                        Title = title,
                        Company = company,
                        Location = location,
                        Country = "Germany",
                        Source = SourceName,
                        JobUrl = url,
                        Description = description,
                        PostedDate = DateTime.UtcNow,
                        DiscoveredAt = DateTime.UtcNow,
                        Status = "Discovered"
                    };

                    jobs.Add(job);
                }
            }
            _logger.LogInformation("Fetched {Count} jobs from Arbeitnow Germany.", jobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching jobs from Arbeitnow");
        }

        return jobs;
    }
}
