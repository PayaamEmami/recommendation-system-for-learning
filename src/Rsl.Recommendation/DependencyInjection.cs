using Microsoft.Extensions.DependencyInjection;
using Rsl.Recommendation.Engine;
using Rsl.Recommendation.Filters;
using Rsl.Recommendation.Scorers;
using Rsl.Recommendation.Services;

namespace Rsl.Recommendation;

/// <summary>
/// Extension methods for configuring recommendation services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register all recommendation engine services.
    /// </summary>
    public static IServiceCollection AddRecommendationEngine(this IServiceCollection services)
    {
        // Scorers
        services.AddScoped<IResourceScorer, SourceScorer>();
        services.AddScoped<IResourceScorer, RecencyScorer>();
        services.AddScoped<IResourceScorer, VoteHistoryScorer>();
        services.AddScoped<CompositeScorer>();

        // Filters
        services.AddScoped<IRecommendationFilter, SeenResourceFilter>();
        services.AddScoped<IRecommendationFilter, DiversityFilter>();

        // Services
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IRecommendationEngine, HybridRecommendationEngine>();
        services.AddScoped<IFeedGenerator, FeedGenerator>();

        return services;
    }
}

