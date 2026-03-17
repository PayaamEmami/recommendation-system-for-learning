using Crs.Core.Entities;
using Crs.Core.Interfaces;
using Crs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Crs.Infrastructure.Repositories;

/// <summary>
/// Implementation of IManualContentFeedbackRepository using Entity Framework Core.
/// </summary>
public class ManualContentFeedbackRepository : IManualContentFeedbackRepository
{
    private readonly CrsDbContext _context;

    public ManualContentFeedbackRepository(CrsDbContext context)
    {
        _context = context;
    }

    public async Task<ManualContentFeedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ManualContentFeedback
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<ManualContentFeedback?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ManualContentFeedback
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<ManualContentFeedback>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ManualContentFeedback
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ManualContentFeedback> CreateAsync(ManualContentFeedback feedback, CancellationToken cancellationToken = default)
    {
        _context.ManualContentFeedback.Add(feedback);
        await _context.SaveChangesAsync(cancellationToken);
        return feedback;
    }

    public async Task<ManualContentFeedback> UpdateAsync(ManualContentFeedback feedback, CancellationToken cancellationToken = default)
    {
        _context.ManualContentFeedback.Update(feedback);
        await _context.SaveChangesAsync(cancellationToken);
        return feedback;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var feedback = await _context.ManualContentFeedback.FindAsync(new object[] { id }, cancellationToken);
        if (feedback != null)
        {
            _context.ManualContentFeedback.Remove(feedback);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
