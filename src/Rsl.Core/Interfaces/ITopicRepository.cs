using Rsl.Core.Entities;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for Topic entity operations.
/// </summary>
public interface ITopicRepository
{
    Task<Topic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Topic?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Topic>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Topic>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Topic> CreateAsync(Topic topic, CancellationToken cancellationToken = default);
    Task<Topic> UpdateAsync(Topic topic, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

