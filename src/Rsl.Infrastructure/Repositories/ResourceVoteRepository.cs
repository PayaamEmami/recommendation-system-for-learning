using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;

namespace Rsl.Infrastructure.Repositories;

/// <summary>
/// Implementation of IResourceVoteRepository using Entity Framework Core.
/// </summary>
public class ResourceVoteRepository : IResourceVoteRepository
{
    private readonly RslDbContext _context;

    public ResourceVoteRepository(RslDbContext context)
    {
        _context = context;
    }

    public async Task<ResourceVote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceVotes
            .Include(v => v.User)
            .Include(v => v.Resource)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<ResourceVote?> GetByUserAndResourceAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceVotes
            .Include(v => v.User)
            .Include(v => v.Resource)
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ResourceId == resourceId, cancellationToken);
    }

    public async Task<IEnumerable<ResourceVote>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceVotes
            .Where(v => v.UserId == userId)
            .Include(v => v.Resource)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ResourceVote>> GetByResourceAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceVotes
            .Where(v => v.ResourceId == resourceId)
            .Include(v => v.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResourceVote> CreateAsync(ResourceVote vote, CancellationToken cancellationToken = default)
    {
        _context.ResourceVotes.Add(vote);
        await _context.SaveChangesAsync(cancellationToken);
        return vote;
    }

    public async Task<ResourceVote> UpdateAsync(ResourceVote vote, CancellationToken cancellationToken = default)
    {
        _context.ResourceVotes.Update(vote);
        await _context.SaveChangesAsync(cancellationToken);
        return vote;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vote = await _context.ResourceVotes.FindAsync(new object[] { id }, cancellationToken);
        if (vote != null)
        {
            _context.ResourceVotes.Remove(vote);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUpvoteCountAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceVotes
            .CountAsync(v => v.ResourceId == resourceId && v.VoteType == VoteType.Upvote, cancellationToken);
    }

    public async Task<int> GetDownvoteCountAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceVotes
            .CountAsync(v => v.ResourceId == resourceId && v.VoteType == VoteType.Downvote, cancellationToken);
    }
}

