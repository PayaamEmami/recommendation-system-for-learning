using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Crs.Api.DTOs.Votes.Requests;
using Crs.Api.Services;
using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Core.Interfaces;

namespace Crs.Tests.Unit.Api;

[TestClass]
public sealed class VoteServiceTests
{
    private static VoteService CreateService(
        out Mock<IContentVoteRepository> voteRepository,
        out Mock<IUserRepository> userRepository,
        out Mock<IContentRepository> contentRepository)
    {
        voteRepository = new Mock<IContentVoteRepository>(MockBehavior.Strict);
        userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        contentRepository = new Mock<IContentRepository>(MockBehavior.Strict);
        return new VoteService(voteRepository.Object, userRepository.Object, contentRepository.Object, NullLogger<VoteService>.Instance);
    }

    [TestMethod]
    public async Task VoteOnContentAsync_WhenUserMissing_Throws()
    {
        var service = CreateService(out _, out var userRepository, out _);
        userRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.VoteOnContentAsync(Guid.NewGuid(), Guid.NewGuid(), new VoteRequest { VoteType = VoteType.Upvote }, CancellationToken.None));
    }

    [TestMethod]
    public async Task VoteOnContentAsync_WhenContentMissing_Throws()
    {
        var service = CreateService(out _, out var userRepository, out var contentRepository);
        var userId = Guid.NewGuid();
        userRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });
        contentRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Content?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.VoteOnContentAsync(userId, Guid.NewGuid(), new VoteRequest { VoteType = VoteType.Upvote }, CancellationToken.None));
    }

    [TestMethod]
    public async Task VoteOnContentAsync_WhenExistingVote_Updates()
    {
        var service = CreateService(out var voteRepository, out var userRepository, out var contentRepository);
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();

        userRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });
        contentRepository.Setup(repo => repo.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogPost { Id = contentId });

        var existingVote = new ContentVote
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContentId = contentId,
            VoteType = VoteType.Downvote
        };

        voteRepository.Setup(repo => repo.GetByUserAndContentAsync(userId, contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVote);
        voteRepository.Setup(repo => repo.UpdateAsync(existingVote, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVote);

        var response = await service.VoteOnContentAsync(userId, contentId, new VoteRequest { VoteType = VoteType.Upvote }, CancellationToken.None);

        Assert.AreEqual(VoteType.Upvote, response.VoteType);
        voteRepository.Verify(repo => repo.UpdateAsync(existingVote, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task VoteOnContentAsync_WhenNewVote_Creates()
    {
        var service = CreateService(out var voteRepository, out var userRepository, out var contentRepository);
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();

        userRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });
        contentRepository.Setup(repo => repo.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogPost { Id = contentId });
        voteRepository.Setup(repo => repo.GetByUserAndContentAsync(userId, contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentVote?)null);
        voteRepository.Setup(repo => repo.CreateAsync(It.IsAny<ContentVote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentVote vote, CancellationToken _) => vote);

        var response = await service.VoteOnContentAsync(userId, contentId, new VoteRequest { VoteType = VoteType.Upvote }, CancellationToken.None);

        Assert.AreEqual(VoteType.Upvote, response.VoteType);
        voteRepository.Verify(repo => repo.CreateAsync(It.IsAny<ContentVote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RemoveVoteAsync_WhenMissing_Throws()
    {
        var service = CreateService(out var voteRepository, out _, out _);
        voteRepository.Setup(repo => repo.GetByUserAndContentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentVote?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RemoveVoteAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    [TestMethod]
    public async Task RemoveVoteAsync_WhenExists_Deletes()
    {
        var service = CreateService(out var voteRepository, out _, out _);
        var vote = new ContentVote { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), ContentId = Guid.NewGuid() };
        voteRepository.Setup(repo => repo.GetByUserAndContentAsync(vote.UserId, vote.ContentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vote);
        voteRepository.Setup(repo => repo.DeleteAsync(vote.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await service.RemoveVoteAsync(vote.UserId, vote.ContentId, CancellationToken.None);

        voteRepository.Verify(repo => repo.DeleteAsync(vote.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetUserVotesAsync_ReturnsMappedVotes()
    {
        var service = CreateService(out var voteRepository, out _, out _);
        var userId = Guid.NewGuid();
        voteRepository.Setup(repo => repo.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContentVote>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, ContentId = Guid.NewGuid(), VoteType = VoteType.Upvote }
            });

        var result = await service.GetUserVotesAsync(userId, CancellationToken.None);

        Assert.HasCount(1, result);
        Assert.AreEqual(userId, result[0].UserId);
    }

    [TestMethod]
    public async Task GetUserVoteOnContentAsync_WhenMissing_ReturnsNull()
    {
        var service = CreateService(out var voteRepository, out _, out _);
        voteRepository.Setup(repo => repo.GetByUserAndContentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentVote?)null);

        var result = await service.GetUserVoteOnContentAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsNull(result);
    }
}
