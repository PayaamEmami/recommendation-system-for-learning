using Microsoft.EntityFrameworkCore;
using Crs.Core.Entities;
using Crs.Core.Interfaces;
using Crs.Infrastructure.Data;

namespace Crs.Infrastructure.Repositories;

/// <summary>
/// Implementation of IXConnectionRepository using Entity Framework Core.
/// </summary>
public class XConnectionRepository : IXConnectionRepository
{
    private readonly CrsDbContext _context;

    public XConnectionRepository(CrsDbContext context)
    {
        _context = context;
    }

    public async Task<XConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.XConnections
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<List<XConnection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.XConnections
            .OrderBy(x => x.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<XConnection> UpsertAsync(XConnection connection, CancellationToken cancellationToken = default)
    {
        var existing = await _context.XConnections
            .FirstOrDefaultAsync(x => x.UserId == connection.UserId, cancellationToken);

        if (existing == null)
        {
            connection.ConnectedAt = DateTime.UtcNow;
            connection.UpdatedAt = DateTime.UtcNow;
            _context.XConnections.Add(connection);
            await _context.SaveChangesAsync(cancellationToken);
            return connection;
        }

        existing.XUserId = connection.XUserId;
        existing.Handle = connection.Handle;
        existing.DisplayName = connection.DisplayName;
        existing.AccessTokenEncrypted = connection.AccessTokenEncrypted;
        existing.RefreshTokenEncrypted = connection.RefreshTokenEncrypted;
        existing.TokenExpiresAt = connection.TokenExpiresAt;
        existing.Scopes = connection.Scopes;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.XConnections.Update(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _context.XConnections
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (connection != null)
        {
            _context.XConnections.Remove(connection);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
