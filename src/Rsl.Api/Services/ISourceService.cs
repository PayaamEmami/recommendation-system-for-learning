using Rsl.Api.DTOs.Requests;
using Rsl.Api.DTOs.Responses;
using Rsl.Core.Enums;

namespace Rsl.Api.Services;

/// <summary>
/// Service interface for managing sources.
/// </summary>
public interface ISourceService
{
    /// <summary>
    /// Gets a source by ID.
    /// </summary>
    Task<SourceResponse?> GetSourceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sources for a specific user.
    /// </summary>
    Task<List<SourceResponse>> GetUserSourcesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sources for a specific user.
    /// </summary>
    Task<List<SourceResponse>> GetActiveUserSourcesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sources by category.
    /// </summary>
    Task<List<SourceResponse>> GetSourcesByCategoryAsync(ResourceType category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new source.
    /// </summary>
    Task<SourceResponse> CreateSourceAsync(Guid userId, CreateSourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing source.
    /// </summary>
    Task<SourceResponse> UpdateSourceAsync(Guid id, UpdateSourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a source.
    /// </summary>
    Task DeleteSourceAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk imports multiple sources.
    /// </summary>
    Task<BulkImportResult> BulkImportSourcesAsync(Guid userId, BulkImportSourcesRequest request, CancellationToken cancellationToken = default);
}

