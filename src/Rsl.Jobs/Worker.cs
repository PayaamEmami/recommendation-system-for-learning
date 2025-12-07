using Rsl.Jobs.Jobs;

namespace Rsl.Jobs;

/// <summary>
/// Background worker service that executes scheduled jobs.
/// </summary>
public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _ingestionInterval = TimeSpan.FromHours(24); // Run ingestion every 24 hours
    private readonly TimeSpan _feedGenerationTime = new TimeSpan(2, 0, 0); // Run feed generation at 2 AM

    public Worker(
        IServiceProvider serviceProvider,
        ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background worker service started");

        // Schedule initial runs
        var lastIngestionTime = DateTime.MinValue;
        var lastFeedGenerationDate = DateOnly.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);

                // Check if it's time to run source ingestion
                if (now - lastIngestionTime >= _ingestionInterval)
                {
                    _logger.LogInformation("Starting source ingestion job");

                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var ingestionLogger = scope.ServiceProvider.GetRequiredService<ILogger<SourceIngestionJob>>();
                        var ingestionJob = new SourceIngestionJob(_serviceProvider, ingestionLogger);
                        await ingestionJob.ExecuteAsync(stoppingToken);
                        lastIngestionTime = now;

                        _logger.LogInformation("Source ingestion job completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Source ingestion job failed");
                    }
                }

                // Check if it's time to run daily feed generation (once per day at specified time)
                if (lastFeedGenerationDate < today && now.TimeOfDay >= _feedGenerationTime)
                {
                    _logger.LogInformation("Starting daily feed generation job");

                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var feedLogger = scope.ServiceProvider.GetRequiredService<ILogger<DailyFeedGenerationJob>>();
                        var feedGenerationJob = new DailyFeedGenerationJob(_serviceProvider, feedLogger);
                        await feedGenerationJob.ExecuteAsync(stoppingToken);
                        lastFeedGenerationDate = today;

                        _logger.LogInformation("Daily feed generation job completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Daily feed generation job failed");
                    }
                }

                // Wait for a minute before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background worker loop");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Background worker service stopped");
    }
}

