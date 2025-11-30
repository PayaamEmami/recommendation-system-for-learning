using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rsl.Api.Services;

namespace Rsl.Api.Controllers;

/// <summary>
/// Handles topic-related operations (read-only).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
public class TopicsController : ControllerBase
{
    private readonly ITopicService _topicService;
    private readonly ILogger<TopicsController> _logger;

    public TopicsController(ITopicService topicService, ILogger<TopicsController> logger)
    {
        _topicService = topicService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available topics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all topics.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<DTOs.Responses.TopicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllTopics(CancellationToken cancellationToken)
    {
        var topics = await _topicService.GetAllTopicsAsync(cancellationToken);
        return Ok(topics);
    }

    /// <summary>
    /// Gets a specific topic by ID.
    /// </summary>
    /// <param name="id">The topic's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The topic information.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DTOs.Responses.TopicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTopicById(Guid id, CancellationToken cancellationToken)
    {
        var topic = await _topicService.GetTopicByIdAsync(id, cancellationToken);

        if (topic == null)
        {
            return NotFound();
        }

        return Ok(topic);
    }
}

