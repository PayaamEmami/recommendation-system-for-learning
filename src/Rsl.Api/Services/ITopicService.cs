using Rsl.Api.DTOs.Responses;

namespace Rsl.Api.Services;

/// <summary>
/// Service interface for topic-related operations.
/// </summary>
public interface ITopicService
{
    /// <summary>
    /// Gets all topics.
    /// </summary>
    Task<List<TopicResponse>> GetAllTopicsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a topic by ID.
    /// </summary>
    Task<TopicResponse?> GetTopicByIdAsync(Guid topicId, CancellationToken cancellationToken = default);
}

