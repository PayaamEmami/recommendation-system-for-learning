using Crs.Api.DTOs.Common;
using Crs.Api.DTOs.Resources.Requests;
using Crs.Api.DTOs.Resources.Responses;
using Crs.Core.Enums;

namespace Crs.Api.Services;

/// <summary>
/// Service interface for resource-related operations.
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Gets paginated resources with optional filtering.
    /// </summary>
    Task<PagedResponse<ResourceResponse>> GetResourcesAsync(
        int pageNumber,
        int pageSize,
        ResourceType? type = null,
        List<Guid>? sourceIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resource by ID.
    /// </summary>
    Task<ResourceResponse?> GetResourceByIdAsync(Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new resource.
    /// </summary>
    Task<ResourceResponse> CreateResourceAsync(CreateResourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing resource.
    /// </summary>
    Task<ResourceResponse> UpdateResourceAsync(Guid resourceId, UpdateResourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a resource.
    /// </summary>
    Task DeleteResourceAsync(Guid resourceId, CancellationToken cancellationToken = default);
}

