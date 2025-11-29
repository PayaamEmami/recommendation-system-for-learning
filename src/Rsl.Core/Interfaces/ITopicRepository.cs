using Rsl.Core.Entities;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for Topic entity operations.
/// Topics are database-driven and pre-seeded. They are not created/modified through the application.
/// Users select from existing topics to indicate their interests.
/// </summary>
public interface ITopicRepository
{
    Task<Topic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Topic>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Topic>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

