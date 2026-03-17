using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Crs.Api.DTOs.Ingestion.Requests;
using Crs.Api.DTOs.Content.Requests;
using Crs.Api.DTOs.Content.Responses;
using Crs.Api.Extensions;
using Crs.Api.Services;
using Crs.Llm.Services;

namespace Crs.Api.Controllers;

/// <summary>
/// Controller for triggering LLM-based content ingestion from sources.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IIngestionAgent _ingestionAgent;
    private readonly ISourceService _sourceService;
    private readonly IContentService _contentService;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(
        IIngestionAgent ingestionAgent,
        ISourceService sourceService,
        IContentService contentService,
        ILogger<IngestionController> logger)
    {
        _ingestionAgent = ingestionAgent;
        _sourceService = sourceService;
        _contentService = contentService;
        _logger = logger;
    }

    /// <summary>
    /// Ingests content from a URL using the LLM agent.
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
            newContent = result.NewContent,
            duplicatesSkipped = result.DuplicatesSkipped,
            content = result.Content.Select(r => new
            {
                r.Title,
                r.Url,
                r.Description,
                type = r.Type.ToString()
            })
        });
    }

    /// <summary>
    /// Ingests and saves content from a source by source ID.
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

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Ingestion failed",
                error = result.ErrorMessage
            });
        }

        // Save new content to database
        var savedContent = new List<ContentResponse>();
        foreach (var extractedContent in result.Content)
        {
            try
            {
                var createRequest = new CreateContentRequest
                {
                    Title = extractedContent.Title,
                    Url = extractedContent.Url,
                    Description = extractedContent.Description,
                    SourceId = sourceId,
                    ContentType = extractedContent.Type
                };

                var saved = await _contentService.CreateContentAsync(createRequest, cancellationToken);
                savedContent.Add(saved);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save content: {Title}", extractedContent.Title);
            }
        }

        // Log successful ingestion
        _logger.LogInformation(
            "Successfully ingested {Count} content from source {SourceId}",
            savedContent.Count, sourceId);

        return Ok(new
        {
            success = true,
            totalFound = result.TotalFound,
            newContent = result.NewContent,
            duplicatesSkipped = result.DuplicatesSkipped,
            savedCount = savedContent.Count,
            content = savedContent
        });
    }

}

