using Microsoft.EntityFrameworkCore;
using Crs.Core.Entities;
using Crs.Core.Interfaces;
using Crs.Infrastructure.Data;

namespace Crs.Infrastructure.Repositories;

/// <summary>
/// Implementation of IXAuthStateRepository.
/// </summary>
public class XAuthStateRepository : IXAuthStateRepository
{
    private readonly CrsDbContext _context;

    public XAuthStateRepository(CrsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(XAuthState state, CancellationToken cancellationToken = default)
    {
        _context.XAuthStates.Add(state);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<XAuthState?> GetAndRemoveAsync(string state, CancellationToken cancellationToken = default)
    {
        var entity = await _context.XAuthStates
            .FirstOrDefaultAsync(x => x.State == state, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        _context.XAuthStates.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;
        await _context.XAuthStates
            .Where(x => x.ExpiresAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
