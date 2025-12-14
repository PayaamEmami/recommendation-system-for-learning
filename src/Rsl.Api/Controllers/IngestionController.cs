using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsl.Api.DTOs.Requests;
using Rsl.Api.DTOs.Responses;
using Rsl.Api.Extensions;
using Rsl.Api.Services;
using Rsl.Llm.Services;

namespace Rsl.Api.Controllers;

/// <summary>
/// Controller for triggering LLM-based resource ingestion from sources.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IIngestionAgent _ingestionAgent;
    private readonly ISourceService _sourceService;
    private readonly IResourceService _resourceService;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(
        IIngestionAgent ingestionAgent,
        ISourceService sourceService,
        IResourceService resourceService,
        ILogger<IngestionController> logger)
    {
        _ingestionAgent = ingestionAgent;
        _sourceService = sourceService;
        _resourceService = resourceService;
        _logger = logger;
    }

    /// <summary>
    /// Ingests resources from a URL using the LLM agent.
    /// </summary>
    [HttpPost("ingest-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IngestFromUrl(
        [FromBody] IngestUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return BadRequest(new { message = "Invalid URL provided." });
        }

        _logger.LogInformation("Starting ingestion from URL: {Url}", request.Url);

        var result = await _ingestionAgent.IngestFromUrlAsync(
            request.Url,
            sourceId: null,
            cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Ingestion failed: {Error}", result.ErrorMessage);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Ingestion failed",
                error = result.ErrorMessage
            });
        }

        return Ok(new
        {
            success = true,
            totalFound = result.TotalFound,
            newResources = result.NewResources,
            duplicatesSkipped = result.DuplicatesSkipped,
            resources = result.Resources.Select(r => new
            {
                r.Title,
                r.Url,
                r.Description,
                type = r.Type.ToString(),
                r.PublishedDate,
                r.Author,
                r.Channel
            })
        });
    }

    /// <summary>
    /// Ingests and saves resources from a source by source ID.
    /// </summary>
    [HttpPost("ingest-source/{sourceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestFromSource(
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "User ID not found in token." });
        }

        // Get the source and verify ownership
        var source = await _sourceService.GetSourceByIdAsync(sourceId, cancellationToken);
        if (source == null)
        {
            return NotFound(new { message = $"Source with ID {sourceId} not found." });
        }

        if (source.UserId != userId)
        {
            return Forbid();
        }

        _logger.LogInformation("Starting ingestion from source {SourceId}: {SourceUrl}",
            sourceId, source.Url);

        // Run the LLM agent
        var result = await _ingestionAgent.IngestFromUrlAsync(
            source.Url,
            sourceId,
            cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Ingestion failed: {Error}", result.ErrorMessage);

            // Update source with error
            source.LastFetchError = result.ErrorMessage;
            await _sourceService.UpdateSourceAsync(sourceId, new UpdateSourceRequest
            {
                Name = source.Name,
                Url = source.Url,
                Description = source.Description,
                Category = source.Category,
                IsActive = source.IsActive
            }, cancellationToken);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Ingestion failed",
                error = result.ErrorMessage
            });
        }

        // Save new resources to database
        var savedResources = new List<ResourceResponse>();
        foreach (var extractedResource in result.Resources)
        {
            try
            {
                var createRequest = new CreateResourceRequest
                {
                    Title = extractedResource.Title,
                    Url = extractedResource.Url,
                    Description = extractedResource.Description,
                    PublishedDate = extractedResource.PublishedDate,
                    SourceId = sourceId,
                    ResourceType = extractedResource.Type
                };

                var saved = await _resourceService.CreateResourceAsync(createRequest, cancellationToken);
                savedResources.Add(saved);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save resource: {Title}", extractedResource.Title);
            }
        }

        // Update source last fetched timestamp
        source.LastFetchedAt = DateTime.UtcNow;
        source.LastFetchError = null;
        await _sourceService.UpdateSourceAsync(sourceId, new UpdateSourceRequest
        {
            Name = source.Name,
            Url = source.Url,
            Description = source.Description,
            Category = source.Category,
            IsActive = source.IsActive
        }, cancellationToken);

        _logger.LogInformation("Ingestion completed: {Saved} resources saved", savedResources.Count);

        return Ok(new
        {
            success = true,
            totalFound = result.TotalFound,
            newResources = result.NewResources,
            duplicatesSkipped = result.DuplicatesSkipped,
            savedCount = savedResources.Count,
            resources = savedResources
        });
    }

}

