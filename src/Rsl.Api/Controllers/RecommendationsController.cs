using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rsl.Api.DTOs.Recommendations.Responses;
using Rsl.Api.Extensions;
using Rsl.Api.Services;
using Rsl.Core.Enums;

namespace Rsl.Api.Controllers;

/// <summary>
/// Handles recommendation-related operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets today's recommendations across all feed types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recommendations grouped by feed type.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<FeedRecommendationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTodaysRecommendations(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var recommendations = await _recommendationService.GetTodaysRecommendationsAsync(
            userId.Value,
            cancellationToken);

        return Ok(recommendations);
    }

    /// <summary>
    /// Gets recommendations for a specific feed type.
    /// </summary>
    /// <param name="feedType">The feed type (Papers, Videos, etc.).</param>
    /// <param name="date">Optional date (defaults to today).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recommendations for the specified feed type and date.</returns>
    [HttpGet("{feedType}")]
    [ProducesResponseType(typeof(FeedRecommendationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeedRecommendations(
        ResourceType feedType,
        [FromQuery] DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var recommendations = await _recommendationService.GetFeedRecommendationsAsync(
            userId.Value,
            feedType,
            targetDate,
            cancellationToken);

        return Ok(recommendations);
    }
}

