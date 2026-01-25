using Rsl.Core.Entities;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for managing XConnection entities.
/// </summary>
public interface IXConnectionRepository
{
    Task<XConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<XConnection>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<XConnection> UpsertAsync(XConnection connection, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
