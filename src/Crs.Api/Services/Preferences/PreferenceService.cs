using Crs.Api.DTOs.Preferences.Requests;
using Crs.Api.DTOs.Preferences.Responses;
using Crs.Core.Entities;
using Crs.Core.Interfaces;

namespace Crs.Api.Services;

public class PreferenceService : IPreferenceService
{
    private readonly IManualContentFeedbackRepository _manualContentFeedbackRepository;
    private readonly ILogger<PreferenceService> _logger;

    public PreferenceService(
        IManualContentFeedbackRepository manualContentFeedbackRepository,
        ILogger<PreferenceService> logger)
    {
        _manualContentFeedbackRepository = manualContentFeedbackRepository;
        _logger = logger;
    }

    public async Task<List<ManualContentFeedbackResponse>> GetManualFeedbackAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var feedback = await _manualContentFeedbackRepository.GetByUserAsync(userId, cancellationToken);
        return feedback.Select(MapToResponse).ToList();
    }

    public async Task<ManualContentFeedbackResponse?> GetManualFeedbackByIdAsync(Guid userId, Guid feedbackId, CancellationToken cancellationToken = default)
    {
        var feedback = await _manualContentFeedbackRepository.GetByIdForUserAsync(feedbackId, userId, cancellationToken);
        return feedback == null ? null : MapToResponse(feedback);
    }

    public async Task<ManualContentFeedbackResponse> CreateManualFeedbackAsync(Guid userId, CreateManualContentFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var feedback = new ManualContentFeedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim(),
            ContentType = request.ContentType,
            VoteType = request.VoteType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        feedback = await _manualContentFeedbackRepository.CreateAsync(feedback, cancellationToken);

        _logger.LogInformation("Created manual content feedback {FeedbackId} for user {UserId}", feedback.Id, userId);

        return MapToResponse(feedback);
    }

    public async Task<ManualContentFeedbackResponse> UpdateManualFeedbackAsync(Guid userId, Guid feedbackId, UpdateManualContentFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var feedback = await _manualContentFeedbackRepository.GetByIdForUserAsync(feedbackId, userId, cancellationToken);
        if (feedback == null)
        {
            throw new KeyNotFoundException($"Manual content feedback with ID {feedbackId} not found");
        }

        feedback.Title = request.Title.Trim();
        feedback.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        feedback.Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim();
        feedback.ContentType = request.ContentType;
        feedback.VoteType = request.VoteType;
        feedback.UpdatedAt = DateTime.UtcNow;

        feedback = await _manualContentFeedbackRepository.UpdateAsync(feedback, cancellationToken);

        _logger.LogInformation("Updated manual content feedback {FeedbackId} for user {UserId}", feedback.Id, userId);

        return MapToResponse(feedback);
    }

    public async Task DeleteManualFeedbackAsync(Guid userId, Guid feedbackId, CancellationToken cancellationToken = default)
    {
        var feedback = await _manualContentFeedbackRepository.GetByIdForUserAsync(feedbackId, userId, cancellationToken);
        if (feedback == null)
        {
            throw new KeyNotFoundException($"Manual content feedback with ID {feedbackId} not found");
        }

        await _manualContentFeedbackRepository.DeleteAsync(feedbackId, cancellationToken);

        _logger.LogInformation("Deleted manual content feedback {FeedbackId} for user {UserId}", feedbackId, userId);
    }

    private static ManualContentFeedbackResponse MapToResponse(ManualContentFeedback feedback)
    {
        return new ManualContentFeedbackResponse
        {
            Id = feedback.Id,
            UserId = feedback.UserId,
            Title = feedback.Title,
            Description = feedback.Description,
            Url = feedback.Url,
            ContentType = feedback.ContentType,
            VoteType = feedback.VoteType,
            CreatedAt = feedback.CreatedAt,
            UpdatedAt = feedback.UpdatedAt
        };
    }
}
