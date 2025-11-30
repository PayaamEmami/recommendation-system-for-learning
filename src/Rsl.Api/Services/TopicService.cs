using Rsl.Api.DTOs.Responses;
using Rsl.Core.Interfaces;

namespace Rsl.Api.Services;

/// <summary>
/// Service for handling topic-related operations.
/// </summary>
public class TopicService : ITopicService
{
    private readonly ITopicRepository _topicRepository;
    private readonly ILogger<TopicService> _logger;

    public TopicService(ITopicRepository topicRepository, ILogger<TopicService> logger)
    {
        _topicRepository = topicRepository;
        _logger = logger;
    }

    public async Task<List<TopicResponse>> GetAllTopicsAsync(CancellationToken cancellationToken = default)
    {
        var topics = await _topicRepository.GetAllAsync(cancellationToken);

        return topics.Select(t => new TopicResponse
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public async Task<TopicResponse?> GetTopicByIdAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        var topic = await _topicRepository.GetByIdAsync(topicId, cancellationToken);

        if (topic == null)
        {
            return null;
        }

        return new TopicResponse
        {
            Id = topic.Id,
            Name = topic.Name,
            Description = topic.Description,
            CreatedAt = topic.CreatedAt
        };
    }
}

