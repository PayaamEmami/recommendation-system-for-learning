using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;

namespace Rsl.Infrastructure.Repositories;

/// <summary>
/// Implementation of ITopicRepository using Entity Framework Core.
/// </summary>
public class TopicRepository : ITopicRepository
{
    private readonly RslDbContext _context;

    public TopicRepository(RslDbContext context)
    {
        _context = context;
    }

    public async Task<Topic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Topics
            .Include(t => t.InterestedUsers)
            .Include(t => t.Resources)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Topic>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Topics
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Topic>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Topics
            .Where(t => t.InterestedUsers.Any(u => u.Id == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Topics.AnyAsync(t => t.Id == id, cancellationToken);
    }
}

