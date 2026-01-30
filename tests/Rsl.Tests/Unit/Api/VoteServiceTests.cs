using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Api.DTOs.Votes.Requests;
using Rsl.Api.Services;
using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class VoteServiceTests
{
    private static VoteService CreateService(
        out Mock<IResourceVoteRepository> voteRepository,
        out Mock<IUserRepository> userRepository,
        out Mock<IResourceRepository> resourceRepository)
    {
        voteRepository = new Mock<IResourceVoteRepository>(MockBehavior.Strict);
        userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        resourceRepository = new Mock<IResourceRepository>(MockBehavior.Strict);
        return new VoteService(voteRepository.Object, userRepository.Object, resourceRepository.Object, NullLogger<VoteService>.Instance);
    }

    [TestMethod]
    public async Task VoteOnResourceAsync_WhenUserMissing_Throws()
    {
        var service = CreateService(out _, out var userRepository, out _);
        userRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.VoteOnResourceAsync(Guid.NewGuid(), Guid.NewGuid(), new VoteRequest { VoteType = VoteType.Upvote }, CancellationToken.None));
    }

    [TestMethod]
    public async Task VoteOnResourceAsync_WhenResourceMissing_Throws()
    {
        var service = CreateService(out _, out var userRepository, out var resourceRepository);
        var userId = Guid.NewGuid();
        userRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });
        resourceRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.VoteOnResourceAsync(userId, Guid.NewGuid(), new VoteRequest { VoteType = VoteType.Upvote }, CancellationToken.None));
    }

    [TestMethod]
    public async Task VoteOnResourceAsync_WhenExistingVote_Updates()
    {
        var service = CreateService(out var voteRepository, out var userRepository, out var resourceRepository);
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        userRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });
        resourceRepository.Setup(repo => repo.GetByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogPost { Id = resourceId });

        var existingVote = new ResourceVote
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResourceId = resourceId,
            VoteType = VoteType.Downvote
        };

        voteRepository.Setup(repo => repo.GetByUserAndResourceAsync(userId, resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVote);
        voteRepository.Setup(repo => repo.UpdateAsync(existingVote, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVote);

        var response = await service.VoteOnResourceAsync(userId, resourceId, new VoteRequest { VoteType = VoteType.Upvote }, CancellationToken.None);

        Assert.AreEqual(VoteType.Upvote, response.VoteType);
        voteRepository.Verify(repo => repo.UpdateAsync(existingVote, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task VoteOnResourceAsync_WhenNewVote_Creates()
    {
        var service = CreateService(out var voteRepository, out var userRepository, out var resourceRepository);
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        userRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });
        resourceRepository.Setup(repo => repo.GetByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogPost { Id = resourceId });
        voteRepository.Setup(repo => repo.GetByUserAndResourceAsync(userId, resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceVote?)null);
        voteRepository.Setup(repo => repo.CreateAsync(It.IsAny<ResourceVote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceVote vote, CancellationToken _) => vote);

        var response = await service.VoteOnResourceAsync(userId, resourceId, new VoteRequest { VoteType = VoteType.Upvote }, CancellationToken.None);

        Assert.AreEqual(VoteType.Upvote, response.VoteType);
        voteRepository.Verify(repo => repo.CreateAsync(It.IsAny<ResourceVote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RemoveVoteAsync_WhenMissing_Throws()
    {
        var service = CreateService(out var voteRepository, out _, out _);
        voteRepository.Setup(repo => repo.GetByUserAndResourceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceVote?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RemoveVoteAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    [TestMethod]
    public async Task RemoveVoteAsync_WhenExists_Deletes()
    {
        var service = CreateService(out var voteRepository, out _, out _);
        var vote = new ResourceVote { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), ResourceId = Guid.NewGuid() };
        voteRepository.Setup(repo => repo.GetByUserAndResourceAsync(vote.UserId, vote.ResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vote);
        voteRepository.Setup(repo => repo.DeleteAsync(vote.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await service.RemoveVoteAsync(vote.UserId, vote.ResourceId, CancellationToken.None);

        voteRepository.Verify(repo => repo.DeleteAsync(vote.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetUserVotesAsync_ReturnsMappedVotes()
    {
        var service = CreateService(out var voteRepository, out _, out _);
        var userId = Guid.NewGuid();
        voteRepository.Setup(repo => repo.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResourceVote>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, ResourceId = Guid.NewGuid(), VoteType = VoteType.Upvote }
            });

        var result = await service.GetUserVotesAsync(userId, CancellationToken.None);

        Assert.HasCount(1, result);
        Assert.AreEqual(userId, result[0].UserId);
    }

    [TestMethod]
    public async Task GetUserVoteOnResourceAsync_WhenMissing_ReturnsNull()
    {
        var service = CreateService(out var voteRepository, out _, out _);
        voteRepository.Setup(repo => repo.GetByUserAndResourceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceVote?)null);

        var result = await service.GetUserVoteOnResourceAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsNull(result);
    }
}
