using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Api.Services;
using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using RecommendationEntity = Rsl.Core.Entities.Recommendation;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class RecommendationServiceTests
{
    private static RecommendationService CreateService(
        out Mock<IRecommendationRepository> recommendationRepository,
        out Mock<IUserRepository> userRepository)
    {
        recommendationRepository = new Mock<IRecommendationRepository>(MockBehavior.Strict);
        userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        return new RecommendationService(recommendationRepository.Object, userRepository.Object, NullLogger<RecommendationService>.Instance);
    }

    [TestMethod]
    public async Task GetFeedRecommendationsAsync_WhenUserMissing_Throws()
    {
        var service = CreateService(out _, out var userRepository);
        userRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetFeedRecommendationsAsync(Guid.NewGuid(), ResourceType.Video, DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None));
    }

    [TestMethod]
    public async Task GetFeedRecommendationsAsync_WhenFallbackUsed_ReturnsMostRecent()
    {
        var service = CreateService(out var recommendationRepository, out var userRepository);
        var userId = Guid.NewGuid();
        userRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });

        var requestedDate = new DateOnly(2024, 12, 1);
        var fallbackDate = new DateOnly(2024, 11, 30);

        recommendationRepository.Setup(repo => repo.GetByUserDateAndTypeAsync(userId, requestedDate, ResourceType.Paper, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RecommendationEntity>());
        recommendationRepository.Setup(repo => repo.GetMostRecentDateWithRecommendationsAsync(userId, ResourceType.Paper, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackDate);

        var recommendations = new List<RecommendationEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Position = 2,
                FeedType = ResourceType.Paper,
                Date = fallbackDate,
                Resource = new Paper { Id = Guid.NewGuid(), Title = "Two", Url = "https://example.com/2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Position = 1,
                FeedType = ResourceType.Paper,
                Date = fallbackDate,
                Resource = new Paper { Id = Guid.NewGuid(), Title = "One", Url = "https://example.com/1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            }
        };

        recommendationRepository.Setup(repo => repo.GetByUserDateAndTypeAsync(userId, fallbackDate, ResourceType.Paper, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recommendations);

        var response = await service.GetFeedRecommendationsAsync(userId, ResourceType.Paper, requestedDate, CancellationToken.None);

        Assert.AreEqual(fallbackDate, response.Date);
        Assert.HasCount(2, response.Recommendations);
        Assert.AreEqual(1, response.Recommendations[0].Position);
    }

    [TestMethod]
    public async Task GetTodaysRecommendationsAsync_WhenUserMissing_Throws()
    {
        var service = CreateService(out _, out var userRepository);
        userRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetTodaysRecommendationsAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [TestMethod]
    public async Task GetTodaysRecommendationsAsync_ReturnsOnlyFeedsWithData()
    {
        var service = CreateService(out var recommendationRepository, out var userRepository);
        var userId = Guid.NewGuid();
        userRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        recommendationRepository.Setup(repo => repo.GetByUserDateAndTypeAsync(userId, today, It.IsAny<ResourceType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RecommendationEntity>());
        recommendationRepository.Setup(repo => repo.GetMostRecentDateWithRecommendationsAsync(userId, It.IsAny<ResourceType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly?)null);

        var recommendation = new RecommendationEntity
        {
            Id = Guid.NewGuid(),
            Position = 1,
            FeedType = ResourceType.Video,
            Date = today,
            Resource = new Video { Id = Guid.NewGuid(), Title = "Video", Url = "https://example.com/video", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        recommendationRepository.Setup(repo => repo.GetByUserDateAndTypeAsync(userId, today, ResourceType.Video, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { recommendation });

        var result = await service.GetTodaysRecommendationsAsync(userId, CancellationToken.None);

        Assert.HasCount(1, result);
        Assert.AreEqual(ResourceType.Video, result[0].FeedType);
    }
}
