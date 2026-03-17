using Crs.Api.DTOs.Common;
using Crs.Api.DTOs.Content.Requests;
using Crs.Api.DTOs.Content.Responses;
using Crs.Core.Enums;

namespace Crs.Api.Services;

/// <summary>
/// Service interface for content-related operations.
/// </summary>
public interface IContentService
{
    /// <summary>
    /// Gets paginated content with optional filtering.
    /// </summary>
    Task<PagedResponse<ContentResponse>> GetContentAsync(
        int pageNumber,
        int pageSize,
        ContentType? type = null,
        List<Guid>? sourceIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a content item by ID.
    /// </summary>
    Task<ContentResponse?> GetContentByIdAsync(Guid contentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new content item.
    /// </summary>
    Task<ContentResponse> CreateContentAsync(CreateContentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing content.
    /// </summary>
    Task<ContentResponse> UpdateContentAsync(Guid contentId, UpdateContentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a content item.
    /// </summary>
    Task DeleteContentAsync(Guid contentId, CancellationToken cancellationToken = default);
}
