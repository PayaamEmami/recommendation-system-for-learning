using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;

namespace Rsl.Infrastructure.Repositories;

/// <summary>
/// Implementation of IXFollowedAccountRepository using Entity Framework Core.
/// </summary>
public class XFollowedAccountRepository : IXFollowedAccountRepository
{
    private readonly RslDbContext _context;

    public XFollowedAccountRepository(RslDbContext context)
    {
        _context = context;
    }

    public async Task<List<XFollowedAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.XFollowedAccounts
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Handle)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForUserAsync(Guid userId, List<XFollowedAccount> accounts, CancellationToken cancellationToken = default)
    {
        var existing = await _context.XFollowedAccounts
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var incomingByXId = accounts.ToDictionary(x => x.XUserId, StringComparer.Ordinal);
        var existingByXId = existing.ToDictionary(x => x.XUserId, StringComparer.Ordinal);

        foreach (var existingAccount in existing)
        {
            if (!incomingByXId.ContainsKey(existingAccount.XUserId))
            {
                _context.XFollowedAccounts.Remove(existingAccount);
            }
        }

        foreach (var incoming in accounts)
        {
            if (existingByXId.TryGetValue(incoming.XUserId, out var match))
            {
                match.Handle = incoming.Handle;
                match.DisplayName = incoming.DisplayName;
                match.ProfileImageUrl = incoming.ProfileImageUrl;
                match.FollowedAt = incoming.FollowedAt;
            }
            else
            {
                incoming.UserId = userId;
                _context.XFollowedAccounts.Add(incoming);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
