using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;

namespace Rsl.Tests.Unit.Infrastructure;

public sealed class InMemoryResourceVoteRepository : IResourceVoteRepository
{
    private readonly List<ResourceVote> _votes = new();

    public Task<ResourceVote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_votes.FirstOrDefault(v => v.Id == id));
    }

    public Task<ResourceVote?> GetByUserAndResourceAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_votes.FirstOrDefault(v => v.UserId == userId && v.ResourceId == resourceId));
    }

    public Task<IEnumerable<ResourceVote>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ResourceVote>>(_votes.Where(v => v.UserId == userId).ToList());
    }

    public Task<IEnumerable<ResourceVote>> GetByResourceAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ResourceVote>>(_votes.Where(v => v.ResourceId == resourceId).ToList());
    }

    public Task<ResourceVote> CreateAsync(ResourceVote vote, CancellationToken cancellationToken = default)
    {
        _votes.Add(vote);
        return Task.FromResult(vote);
    }

    public Task<ResourceVote> UpdateAsync(ResourceVote vote, CancellationToken cancellationToken = default)
    {
        var index = _votes.FindIndex(v => v.Id == vote.Id);
        if (index >= 0)
        {
            _votes[index] = vote;
        }
        else
        {
            _votes.Add(vote);
        }

        return Task.FromResult(vote);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _votes.RemoveAll(v => v.Id == id);
        return Task.CompletedTask;
    }

    public Task<int> GetUpvoteCountAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        var count = _votes.Count(v => v.ResourceId == resourceId && v.VoteType == VoteType.Upvote);
        return Task.FromResult(count);
    }

    public Task<int> GetDownvoteCountAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        var count = _votes.Count(v => v.ResourceId == resourceId && v.VoteType == VoteType.Downvote);
        return Task.FromResult(count);
    }
}
