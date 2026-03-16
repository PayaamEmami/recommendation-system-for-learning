using Microsoft.EntityFrameworkCore;
using Crs.Core.Entities;
using Crs.Core.Interfaces;
using Crs.Infrastructure.Data;

namespace Crs.Infrastructure.Repositories;

/// <summary>
/// Implementation of IRefreshTokenRepository.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CrsDbContext _context;

    public RefreshTokenRepository(CrsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        _context.RefreshTokens.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(x => x.ExpiresAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
