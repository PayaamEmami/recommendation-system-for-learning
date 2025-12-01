using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;

namespace Rsl.Infrastructure.Repositories;

/// <summary>
/// Implementation of IResourceRepository using Entity Framework Core.
/// </summary>
public class ResourceRepository : IResourceRepository
{
    private readonly RslDbContext _context;

    public ResourceRepository(RslDbContext context)
    {
        _context = context;
    }

    public async Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .Include(r => r.Source)
            .Include(r => r.Votes)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Resource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .Include(r => r.Source)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Resource>> GetByTypeAsync(ResourceType type, CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .Where(r => r.Type == type)
            .Include(r => r.Source)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Resource>> GetByTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        // Topics no longer exist - return empty list for backward compatibility
        return await Task.FromResult(Enumerable.Empty<Resource>());
    }

    public async Task<IEnumerable<Resource>> GetByTopicsAsync(IEnumerable<Guid> topicIds, CancellationToken cancellationToken = default)
    {
        // Topics no longer exist - return empty list for backward compatibility
        return await Task.FromResult(Enumerable.Empty<Resource>());
    }

    public async Task<Resource> CreateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync(cancellationToken);
        return resource;
    }

    public async Task<Resource> UpdateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        _context.Resources.Update(resource);
        await _context.SaveChangesAsync(cancellationToken);
        return resource;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resource = await _context.Resources.FindAsync(new object[] { id }, cancellationToken);
        if (resource != null)
        {
            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Resources.AnyAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _context.Resources.AnyAsync(r => r.Url == url, cancellationToken);
    }

    public async Task AddAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

