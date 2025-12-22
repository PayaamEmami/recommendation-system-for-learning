using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;

namespace Rsl.Infrastructure.Repositories;

/// <summary>
/// Implementation of the Source repository.
/// </summary>
public class SourceRepository : ISourceRepository
{
    private readonly RslDbContext _context;

    public SourceRepository(RslDbContext context)
    {
        _context = context;
    }

    public async Task<Source?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<Source>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Source>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Source>> GetByCategoryAsync(ResourceType category, CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .Where(s => s.Category == category && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Source>> GetActiveSourcesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .Where(s => s.IsActive)
            .Include(s => s.User)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Source> AddAsync(Source source, CancellationToken cancellationToken = default)
    {
        source.CreatedAt = DateTime.UtcNow;
        source.UpdatedAt = DateTime.UtcNow;

        _context.Sources.Add(source);
        await _context.SaveChangesAsync(cancellationToken);

        return source;
    }

    public async Task UpdateAsync(Source source, CancellationToken cancellationToken = default)
    {
        source.UpdatedAt = DateTime.UtcNow;

        _context.Sources.Update(source);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await _context.Sources.FindAsync(new object[] { id }, cancellationToken);
        if (source != null)
        {
            _context.Sources.Remove(source);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> UrlExistsForUserAsync(Guid userId, string url, CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .AnyAsync(s => s.UserId == userId && s.Url == url, cancellationToken);
    }
}

