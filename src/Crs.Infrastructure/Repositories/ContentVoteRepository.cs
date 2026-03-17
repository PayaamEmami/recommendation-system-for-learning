using Microsoft.EntityFrameworkCore;
using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Core.Interfaces;
using Crs.Infrastructure.Data;

namespace Crs.Infrastructure.Repositories;

/// <summary>
/// Implementation of IContentVoteRepository using Entity Framework Core.
/// </summary>
public class ContentVoteRepository : IContentVoteRepository
{
    private readonly CrsDbContext _context;

    public ContentVoteRepository(CrsDbContext context)
    {
        _context = context;
    }

    public async Task<ContentVote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ContentVotes
            .Include(v => v.User)
            .Include(v => v.Content)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<ContentVote?> GetByUserAndContentAsync(Guid userId, Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentVotes
            .Include(v => v.User)
            .Include(v => v.Content)
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ContentId == contentId, cancellationToken);
    }

    public async Task<IEnumerable<ContentVote>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentVotes
            .Where(v => v.UserId == userId)
            .Include(v => v.Content)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ContentVote>> GetByContentAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentVotes
            .Where(v => v.ContentId == contentId)
            .Include(v => v.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentVote> CreateAsync(ContentVote vote, CancellationToken cancellationToken = default)
    {
        _context.ContentVotes.Add(vote);
        await _context.SaveChangesAsync(cancellationToken);
        return vote;
    }

    public async Task<ContentVote> UpdateAsync(ContentVote vote, CancellationToken cancellationToken = default)
    {
        _context.ContentVotes.Update(vote);
        await _context.SaveChangesAsync(cancellationToken);
        return vote;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vote = await _context.ContentVotes.FindAsync(new object[] { id }, cancellationToken);
        if (vote != null)
        {
            _context.ContentVotes.Remove(vote);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUpvoteCountAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentVotes
            .CountAsync(v => v.ContentId == contentId && v.VoteType == VoteType.Upvote, cancellationToken);
    }

    public async Task<int> GetDownvoteCountAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentVotes
            .CountAsync(v => v.ContentId == contentId && v.VoteType == VoteType.Downvote, cancellationToken);
    }
}

