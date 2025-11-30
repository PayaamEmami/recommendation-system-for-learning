using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rsl.Api.DTOs.Requests;
using Rsl.Api.Extensions;
using Rsl.Api.Services;
using Rsl.Core.Enums;

namespace Rsl.Api.Controllers;

/// <summary>
/// Handles resource-related operations (CRUD).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly IVoteService _voteService;
    private readonly ILogger<ResourcesController> _logger;

    public ResourcesController(
        IResourceService resourceService,
        IVoteService voteService,
        ILogger<ResourcesController> logger)
    {
        _resourceService = resourceService;
        _voteService = voteService;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated resources with optional filtering.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="type">Filter by resource type.</param>
    /// <param name="topicIds">Filter by topic IDs (comma-separated).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of resources.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DTOs.Responses.PagedResponse<DTOs.Responses.ResourceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetResources(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ResourceType? type = null,
        [FromQuery] string? topicIds = null,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
        {
            return BadRequest("Page number must be greater than 0");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Page size must be between 1 and 100");
        }

        // Parse topic IDs if provided
        List<Guid>? topicIdList = null;
        if (!string.IsNullOrWhiteSpace(topicIds))
        {
            try
            {
                topicIdList = topicIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList();
            }
            catch
            {
                return BadRequest("Invalid topic IDs format");
            }
        }

        var resources = await _resourceService.GetResourcesAsync(
            pageNumber,
            pageSize,
            type,
            topicIdList,
            cancellationToken);

        return Ok(resources);
    }

    /// <summary>
    /// Gets a specific resource by ID.
    /// </summary>
    /// <param name="id">The resource's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resource information.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DTOs.Responses.ResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResourceById(Guid id, CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetResourceByIdAsync(id, cancellationToken);

        if (resource == null)
        {
            return NotFound();
        }

        return Ok(resource);
    }

    /// <summary>
    /// Creates a new resource.
    /// </summary>
    /// <param name="request">The resource information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created resource.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DTOs.Responses.ResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateResource(
        [FromBody] CreateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await _resourceService.CreateResourceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetResourceById), new { id = resource.Id }, resource);
    }

    /// <summary>
    /// Updates an existing resource.
    /// </summary>
    /// <param name="id">The resource's ID.</param>
    /// <param name="request">The updated resource information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated resource.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DTOs.Responses.ResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateResource(
        Guid id,
        [FromBody] UpdateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await _resourceService.UpdateResourceAsync(id, request, cancellationToken);
        return Ok(resource);
    }

    /// <summary>
    /// Deletes a resource.
    /// </summary>
    /// <param name="id">The resource's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResource(Guid id, CancellationToken cancellationToken)
    {
        await _resourceService.DeleteResourceAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Casts or updates a vote on a resource.
    /// </summary>
    /// <param name="id">The resource's ID.</param>
    /// <param name="request">The vote information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The vote information.</returns>
    [HttpPost("{id}/vote")]
    [ProducesResponseType(typeof(DTOs.Responses.VoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VoteOnResource(
        Guid id,
        [FromBody] VoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var vote = await _voteService.VoteOnResourceAsync(userId.Value, id, request, cancellationToken);
        return Ok(vote);
    }

    /// <summary>
    /// Removes a vote from a resource.
    /// </summary>
    /// <param name="id">The resource's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}/vote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveVote(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        await _voteService.RemoveVoteAsync(userId.Value, id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the current user's vote on a resource.
    /// </summary>
    /// <param name="id">The resource's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The vote information if exists.</returns>
    [HttpGet("{id}/vote")]
    [ProducesResponseType(typeof(DTOs.Responses.VoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVoteOnResource(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var vote = await _voteService.GetUserVoteOnResourceAsync(userId.Value, id, cancellationToken);

        if (vote == null)
        {
            return NotFound();
        }

        return Ok(vote);
    }
}

