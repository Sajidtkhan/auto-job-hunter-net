using JobAutoApplyNET.Models;

namespace JobAutoApplyNET.Services;

public class DailyJobSchedulerService : BackgroundService
{
    private readonly AggregatorJobService _aggregatorService;
    private readonly AppConfig _config;
    private readonly UserProfile _profile;
    private readonly ILogger<DailyJobSchedulerService> _logger;

    public DailyJobSchedulerService(
        AggregatorJobService aggregatorService,
        AppConfig config,
        UserProfile profile,
        ILogger<DailyJobSchedulerService> logger)
    {
        _aggregatorService = aggregatorService;
        _config = config;
        _profile = profile;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyJobSchedulerService started. Daily target run hour: {Hour}:00", _config.DailyRunHour);

        // Initial scan on startup after 5 seconds delay to allow server initialization
        try
        {
            await Task.Delay(5000, stoppingToken);
            _logger.LogInformation("Executing initial startup job scan for Dubai & Germany...");
            await _aggregatorService.ScanAndProcessJobsAsync(_config, _profile, false, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error in initial startup job scan");
        }

        // Daily loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddHours(_config.DailyRunHour);
                if (now >= nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogInformation("Next scheduled daily job search is at: {NextRun} (in {Hours:N1} hours)", nextRun, delay.TotalHours);

                await Task.Delay(delay, stoppingToken);

                _logger.LogInformation("Running automated daily job apply process for Dubai and Germany...");
                await _aggregatorService.ScanAndProcessJobsAsync(_config, _profile, _config.AutoSendEmailApplication, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily job scheduler cycle");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // retry after 10 min
            }
        }
    }
}
