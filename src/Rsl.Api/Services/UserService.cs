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
    private readonly ISourceRepository _sourceRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ISourceRepository sourceRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _sourceRepository = sourceRepository;
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
            Sources = user.Sources.Select(s => new SourceResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                Name = s.Name,
                Url = s.Url,
                Description = s.Description,
                Category = s.Category,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                LastFetchedAt = s.LastFetchedAt,
                LastFetchError = s.LastFetchError,
                ResourceCount = s.Resources?.Count ?? 0
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

}

