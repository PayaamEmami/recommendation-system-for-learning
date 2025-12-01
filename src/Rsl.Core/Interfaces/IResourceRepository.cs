using Rsl.Core.Entities;
using Rsl.Core.Enums;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for Resource entity operations.
/// </summary>
public interface IResourceRepository
{
    Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Resource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Resource>> GetByTypeAsync(ResourceType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Resource>> GetByTopicAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Resource>> GetByTopicsAsync(IEnumerable<Guid> topicIds, CancellationToken cancellationToken = default);
    Task<Resource> CreateAsync(Resource resource, CancellationToken cancellationToken = default);
    Task<Resource> UpdateAsync(Resource resource, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task AddAsync(Resource resource, CancellationToken cancellationToken = default);
}

