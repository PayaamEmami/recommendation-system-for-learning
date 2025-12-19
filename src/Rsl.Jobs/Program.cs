using Rsl.Infrastructure;
using Rsl.Jobs;
using Rsl.Jobs.Jobs;
using Rsl.Llm;
using Rsl.Recommendation;

var builder = Host.CreateApplicationBuilder(args);

// Register services from other layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLlmServices(builder.Configuration);
builder.Services.AddRecommendationEngine();

// Jobs (scoped, resolved inside Worker)
builder.Services.AddScoped<SourceIngestionJob>();
builder.Services.AddScoped<DailyFeedGenerationJob>();

// Register the background worker
builder.Services.AddHostedService<Worker>();

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
    }
}

host.Run();

