using Rsl.Api.DTOs.Requests;
using Rsl.Api.DTOs.Responses;
using Rsl.Core.Interfaces;

namespace Rsl.Api.Services;

/// <summary>
/// Service for handling user-related operations.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ITopicRepository topicRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _topicRepository = topicRepository;
        _logger = logger;
    }

    public async Task<UserDetailResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        return new UserDetailResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            InterestedTopics = user.InterestedTopics.Select(t => new TopicResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList()
        };
    }

    public async Task<UserResponse> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Update fields if provided
        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User {UserId} updated their profile", userId);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    public async Task<List<TopicResponse>> GetUserTopicsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        return user.InterestedTopics.Select(t => new TopicResponse
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public async Task<List<TopicResponse>> UpdateUserTopicsAsync(
        Guid userId,
        UpdateUserTopicsRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Fetch all requested topics
        var topics = new List<Core.Entities.Topic>();
        foreach (var topicId in request.TopicIds)
        {
            var topic = await _topicRepository.GetByIdAsync(topicId, cancellationToken);
            if (topic == null)
            {
                throw new ArgumentException($"Topic with ID {topicId} not found");
            }
            topics.Add(topic);
        }

        // Update user's topics
        user.InterestedTopics = topics;
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User {UserId} updated their topics to {TopicCount} topics", userId, topics.Count);

        return topics.Select(t => new TopicResponse
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        }).ToList();
    }
}

