using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rsl.Infrastructure;
using Rsl.Jobs.Jobs;
using Rsl.Llm;
using Rsl.Recommendation;

var builder = Host.CreateApplicationBuilder(args);

// Register services from other layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLlmServices(builder.Configuration);
builder.Services.AddRecommendationEngine();

// Jobs
builder.Services.AddScoped<SourceIngestionJob>();
builder.Services.AddScoped<DailyFeedGenerationJob>();

var host = builder.Build();

// Initialize vector store index on startup
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Initializing vector store...");
        var vectorStore = scope.ServiceProvider.GetRequiredService<Rsl.Core.Interfaces.IVectorStore>();
        await vectorStore.InitializeAsync();
        logger.LogInformation("Vector store initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize vector store");
        Environment.Exit(1);
    }
}

// Determine which job to run from command-line arguments
var jobName = args.Length > 0 ? args[0] : null;

if (string.IsNullOrWhiteSpace(jobName))
{
    Console.WriteLine("Usage: Rsl.Jobs <job-name>");
    Console.WriteLine("Available jobs:");
    Console.WriteLine("  ingestion     - Run source ingestion job");
    Console.WriteLine("  feed          - Run daily feed generation job");
    Environment.Exit(1);
}

using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting job: {JobName}", jobName);

        switch (jobName.ToLowerInvariant())
        {
            case "ingestion":
                var ingestionJob = scope.ServiceProvider.GetRequiredService<SourceIngestionJob>();
                await ingestionJob.ExecuteAsync(CancellationToken.None);
                logger.LogInformation("Ingestion job completed successfully");
                break;

            case "feed":
                var feedJob = scope.ServiceProvider.GetRequiredService<DailyFeedGenerationJob>();
                await feedJob.ExecuteAsync(CancellationToken.None);
                logger.LogInformation("Feed generation job completed successfully");
                break;

            default:
                logger.LogError("Unknown job name: {JobName}", jobName);
                Console.WriteLine($"Error: Unknown job '{jobName}'");
                Console.WriteLine("Available jobs: ingestion, feed");
                Environment.Exit(1);
                break;
        }

        logger.LogInformation("Job {JobName} exited successfully", jobName);
        Environment.Exit(0);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Job {JobName} failed with error", jobName);
        Environment.Exit(1);
    }
}

