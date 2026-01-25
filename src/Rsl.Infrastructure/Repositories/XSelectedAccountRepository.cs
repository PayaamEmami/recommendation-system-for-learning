using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;

namespace Rsl.Infrastructure.Repositories;

/// <summary>
/// Implementation of IXSelectedAccountRepository using Entity Framework Core.
/// </summary>
public class XSelectedAccountRepository : IXSelectedAccountRepository
{
    private readonly RslDbContext _context;

    public XSelectedAccountRepository(RslDbContext context)
    {
        _context = context;
    }

    public async Task<List<XSelectedAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.XSelectedAccounts
            .Include(x => x.FollowedAccount)
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderBy(x => x.FollowedAccount.Handle)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForUserAsync(Guid userId, List<XSelectedAccount> selectedAccounts, CancellationToken cancellationToken = default)
    {
        var existing = await _context.XSelectedAccounts
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var existingByFollowedId = existing.ToDictionary(x => x.XFollowedAccountId);
        var selectedIds = selectedAccounts
            .Select(x => x.XFollowedAccountId)
            .ToHashSet();

        foreach (var current in existing)
        {
            var shouldBeActive = selectedIds.Contains(current.XFollowedAccountId);
            if (current.IsActive != shouldBeActive)
            {
                current.IsActive = shouldBeActive;
                if (shouldBeActive)
                {
                    current.SelectedAt = DateTime.UtcNow;
                }
            }
        }

        foreach (var selected in selectedAccounts)
        {
            if (existingByFollowedId.ContainsKey(selected.XFollowedAccountId))
            {
                continue;
            }

            selected.UserId = userId;
            selected.IsActive = true;
            if (selected.SelectedAt == default)
            {
                selected.SelectedAt = DateTime.UtcNow;
            }
            _context.XSelectedAccounts.Add(selected);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
