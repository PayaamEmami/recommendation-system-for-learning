using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Core.Interfaces;

namespace Crs.Tests.Unit.Infrastructure;

public sealed class InMemoryContentVoteRepository : IContentVoteRepository
{
    private readonly List<ContentVote> _votes = new();

    public Task<ContentVote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_votes.FirstOrDefault(v => v.Id == id));
    }

    public Task<ContentVote?> GetByUserAndContentAsync(Guid userId, Guid contentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_votes.FirstOrDefault(v => v.UserId == userId && v.ContentId == contentId));
    }

    public Task<IEnumerable<ContentVote>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ContentVote>>(_votes.Where(v => v.UserId == userId).ToList());
    }

    public Task<IEnumerable<ContentVote>> GetByContentAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ContentVote>>(_votes.Where(v => v.ContentId == contentId).ToList());
    }

    public Task<ContentVote> CreateAsync(ContentVote vote, CancellationToken cancellationToken = default)
    {
        _votes.Add(vote);
        return Task.FromResult(vote);
    }

    public Task<ContentVote> UpdateAsync(ContentVote vote, CancellationToken cancellationToken = default)
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

    public Task<int> GetUpvoteCountAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        var count = _votes.Count(v => v.ContentId == contentId && v.VoteType == VoteType.Upvote);
        return Task.FromResult(count);
    }

    public Task<int> GetDownvoteCountAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        var count = _votes.Count(v => v.ContentId == contentId && v.VoteType == VoteType.Downvote);
        return Task.FromResult(count);
    }
}
