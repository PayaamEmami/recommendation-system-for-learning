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
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.FollowedAccount.Handle)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForUserAsync(Guid userId, List<XSelectedAccount> selectedAccounts, CancellationToken cancellationToken = default)
    {
        var existing = await _context.XSelectedAccounts
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existing.Any())
        {
            _context.XSelectedAccounts.RemoveRange(existing);
        }

        foreach (var selected in selectedAccounts)
        {
            selected.UserId = userId;
            if (selected.SelectedAt == default)
            {
                selected.SelectedAt = DateTime.UtcNow;
            }
            _context.XSelectedAccounts.Add(selected);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
