using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;

namespace Rsl.Infrastructure.Repositories;

/// <summary>
/// Implementation of IXPostRepository using Entity Framework Core.
/// </summary>
public class XPostRepository : IXPostRepository
{
    private readonly RslDbContext _context;

    public XPostRepository(RslDbContext context)
    {
        _context = context;
    }

    public async Task<List<XPost>> GetLatestByUserIdAsync(Guid userId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.XPosts
            .Include(p => p.SelectedAccount)
            .ThenInclude(s => s.FollowedAccount)
            .Where(p => p.UserId == userId && p.SelectedAccount.IsActive)
            .OrderByDescending(p => p.PostCreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertRangeAsync(IEnumerable<XPost> posts, CancellationToken cancellationToken = default)
    {
        var postList = posts.ToList();
        if (!postList.Any())
        {
            return;
        }

        var userId = postList.First().UserId;
        var postIds = postList.Select(p => p.PostId).ToList();

        var existing = await _context.XPosts
            .Where(p => p.UserId == userId && postIds.Contains(p.PostId))
            .ToListAsync(cancellationToken);

        var existingById = existing.ToDictionary(p => p.PostId, StringComparer.Ordinal);

        foreach (var incoming in postList)
        {
            if (existingById.TryGetValue(incoming.PostId, out var match))
            {
                match.Text = incoming.Text;
                match.Url = incoming.Url;
                match.PostCreatedAt = incoming.PostCreatedAt;
                match.AuthorXUserId = incoming.AuthorXUserId;
                match.AuthorHandle = incoming.AuthorHandle;
                match.AuthorName = incoming.AuthorName;
                match.AuthorProfileImageUrl = incoming.AuthorProfileImageUrl;
                match.MediaJson = incoming.MediaJson;
                match.LikeCount = incoming.LikeCount;
                match.ReplyCount = incoming.ReplyCount;
                match.RepostCount = incoming.RepostCount;
                match.QuoteCount = incoming.QuoteCount;
                match.IngestedAt = DateTime.UtcNow;
            }
            else
            {
                incoming.IngestedAt = DateTime.UtcNow;
                _context.XPosts.Add(incoming);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PruneOldPostsAsync(Guid userId, int keepPerAccount, CancellationToken cancellationToken = default)
    {
        if (keepPerAccount <= 0)
        {
            return;
        }

        var posts = await _context.XPosts
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PostCreatedAt)
            .ToListAsync(cancellationToken);

        var toRemove = new List<XPost>();

        foreach (var group in posts.GroupBy(p => p.XSelectedAccountId))
        {
            toRemove.AddRange(group.Skip(keepPerAccount));
        }

        if (toRemove.Any())
        {
            _context.XPosts.RemoveRange(toRemove);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
