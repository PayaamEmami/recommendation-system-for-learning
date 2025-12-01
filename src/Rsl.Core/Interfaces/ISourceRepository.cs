using Rsl.Core.Entities;
using Rsl.Core.Enums;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for managing Source entities.
/// </summary>
public interface ISourceRepository
{
    /// <summary>
    /// Gets a source by its unique identifier.
    /// </summary>
    Task<Source?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sources for a specific user.
    /// </summary>
    Task<List<Source>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sources for a specific user.
    /// </summary>
    Task<List<Source>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sources by category.
    /// </summary>
    Task<List<Source>> GetByCategoryAsync(ResourceType category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sources that need to be fetched.
    /// </summary>
    Task<List<Source>> GetActiveSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new source.
    /// </summary>
    Task<Source> AddAsync(Source source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing source.
    /// </summary>
    Task UpdateAsync(Source source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a source by its unique identifier.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a URL already exists for a user.
    /// </summary>
    Task<bool> UrlExistsForUserAsync(Guid userId, string url, CancellationToken cancellationToken = default);
}

