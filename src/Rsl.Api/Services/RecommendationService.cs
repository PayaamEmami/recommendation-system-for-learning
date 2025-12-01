using Rsl.Api.DTOs.Responses;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;

namespace Rsl.Api.Services;

/// <summary>
/// Service for handling recommendation operations.
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        IRecommendationRepository recommendationRepository,
        IUserRepository userRepository,
        ILogger<RecommendationService> logger)
    {
        _recommendationRepository = recommendationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<FeedRecommendationsResponse> GetFeedRecommendationsAsync(
        Guid userId,
        ResourceType feedType,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Get recommendations for this user, feed type, and date
        var recommendations = await _recommendationRepository.GetByUserDateAndTypeAsync(
            userId,
            date,
            feedType,
            cancellationToken);

        return new FeedRecommendationsResponse
        {
            FeedType = feedType,
            Date = date,
            Recommendations = recommendations
                .OrderBy(r => r.Position)
                .Select(r => new RecommendationResponse
                {
                    Id = r.Id,
                    Resource = new ResourceResponse
                    {
                        Id = r.Resource.Id,
                        Title = r.Resource.Title,
                        Description = r.Resource.Description,
                        Url = r.Resource.Url,
                        PublishedDate = r.Resource.PublishedDate,
                        Type = r.Resource.Type,
                        CreatedAt = r.Resource.CreatedAt,
                        UpdatedAt = r.Resource.UpdatedAt,
                        SourceInfo = r.Resource.Source != null ? new SourceResponse
                        {
                            Id = r.Resource.Source.Id,
                            UserId = r.Resource.Source.UserId,
                            Name = r.Resource.Source.Name,
                            Url = r.Resource.Source.Url,
                            Description = r.Resource.Source.Description,
                            Category = r.Resource.Source.Category,
                            IsActive = r.Resource.Source.IsActive,
                            CreatedAt = r.Resource.Source.CreatedAt,
                            UpdatedAt = r.Resource.Source.UpdatedAt,
                            LastFetchedAt = r.Resource.Source.LastFetchedAt,
                            LastFetchError = r.Resource.Source.LastFetchError,
                            ResourceCount = r.Resource.Source.Resources?.Count ?? 0
                        } : null
                    },
                    Position = r.Position,
                    Score = r.Score,
                    GeneratedAt = r.GeneratedAt
                })
                .ToList()
        };
    }

    public async Task<List<FeedRecommendationsResponse>> GetTodaysRecommendationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allFeedTypes = Enum.GetValues<ResourceType>();

        var feedRecommendations = new List<FeedRecommendationsResponse>();

        foreach (var feedType in allFeedTypes)
        {
            var recommendations = await _recommendationRepository.GetByUserDateAndTypeAsync(
                userId,
                today,
                feedType,
                cancellationToken);

            if (recommendations.Any())
            {
                feedRecommendations.Add(new FeedRecommendationsResponse
                {
                    FeedType = feedType,
                    Date = today,
                    Recommendations = recommendations
                        .OrderBy(r => r.Position)
                        .Select(r => new RecommendationResponse
                        {
                            Id = r.Id,
                            Resource = new ResourceResponse
                            {
                                Id = r.Resource.Id,
                                Title = r.Resource.Title,
                                Description = r.Resource.Description,
                                Url = r.Resource.Url,
                                PublishedDate = r.Resource.PublishedDate,
                                Type = r.Resource.Type,
                                CreatedAt = r.Resource.CreatedAt,
                                UpdatedAt = r.Resource.UpdatedAt,
                                SourceInfo = r.Resource.Source != null ? new SourceResponse
                                {
                                    Id = r.Resource.Source.Id,
                                    UserId = r.Resource.Source.UserId,
                                    Name = r.Resource.Source.Name,
                                    Url = r.Resource.Source.Url,
                                    Description = r.Resource.Source.Description,
                                    Category = r.Resource.Source.Category,
                                    IsActive = r.Resource.Source.IsActive,
                                    CreatedAt = r.Resource.Source.CreatedAt,
                                    UpdatedAt = r.Resource.Source.UpdatedAt,
                                    LastFetchedAt = r.Resource.Source.LastFetchedAt,
                                    LastFetchError = r.Resource.Source.LastFetchError,
                                    ResourceCount = r.Resource.Source.Resources?.Count ?? 0
                                } : null
                            },
                            Position = r.Position,
                            Score = r.Score,
                            GeneratedAt = r.GeneratedAt
                        })
                        .ToList()
                });
            }
        }

        return feedRecommendations;
    }
}

