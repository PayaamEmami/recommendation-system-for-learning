using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Recommendation.Services;

namespace Rsl.Jobs.Jobs;

/// <summary>
/// Background job that generates daily personalized feeds for all users.
/// </summary>
public class DailyFeedGenerationJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyFeedGenerationJob> _logger;

    public DailyFeedGenerationJob(
        IServiceProvider serviceProvider,
        ILogger<DailyFeedGenerationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Execute the daily feed generation job for all users.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting daily feed generation job");

        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var feedGenerator = scope.ServiceProvider.GetRequiredService<IFeedGenerator>();

        try
        {
            // Get all users
            var users = await userRepository.GetAllAsync(cancellationToken);
            var usersList = users.ToList();

            if (!usersList.Any())
            {
                _logger.LogInformation("No users found");
                return;
            }

            _logger.LogInformation("Generating feeds for {Count} users", usersList.Count);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            int totalFeedsGenerated = 0;
            int usersProcessed = 0;

            foreach (var user in usersList)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Feed generation job cancelled");
                    break;
                }

                try
                {
                    _logger.LogInformation("Generating feeds for user: {Email}", user.Email);

                    // Generate feeds for all resource types
                    var feedTypes = Enum.GetValues<ResourceType>();
                    int userFeedCount = 0;

                    foreach (var feedType in feedTypes)
                    {
                        try
                        {
                            var recommendations = await feedGenerator.GenerateFeedAsync(
                                user.Id,
                                feedType,
                                today,
                                count: 10, // Generate 10 recommendations per feed
                                cancellationToken);

                            userFeedCount += recommendations.Count;
                            totalFeedsGenerated += recommendations.Count;

                            _logger.LogDebug(
                                "Generated {Count} recommendations for user {Email}, feed type {FeedType}",
                                recommendations.Count,
                                user.Email,
                                feedType);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error generating {FeedType} feed for user {UserId}",
                                feedType,
                                user.Id);
                        }
                    }

                    usersProcessed++;
                    _logger.LogInformation(
                        "Completed feed generation for user {Email}: {Count} total recommendations",
                        user.Email,
                        userFeedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing user {UserId}", user.Id);
                }
            }

            _logger.LogInformation(
                "Daily feed generation job completed: {UsersProcessed} users processed, {TotalFeeds} recommendations generated",
                usersProcessed,
                totalFeedsGenerated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in daily feed generation job");
            throw;
        }
    }

    /// <summary>
    /// Execute the feed generation for a specific user.
    /// Useful for on-demand feed refreshes.
    /// </summary>
    public async Task ExecuteForUserAsync(
        Guid userId,
        DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting feed generation for user {UserId}", userId);

        using var scope = _serviceProvider.CreateScope();
        var feedGenerator = scope.ServiceProvider.GetRequiredService<IFeedGenerator>();

        try
        {
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var recommendations = await feedGenerator.GenerateAllFeedsAsync(
                userId,
                targetDate,
                cancellationToken);

            _logger.LogInformation(
                "Completed feed generation for user {UserId}: {Count} recommendations",
                userId,
                recommendations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating feeds for user {UserId}", userId);
            throw;
        }
    }
}
