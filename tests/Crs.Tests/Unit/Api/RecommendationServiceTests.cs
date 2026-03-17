using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Crs.Api.Services;
using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Core.Interfaces;
using RecommendationEntity = Crs.Core.Entities.Recommendation;

namespace Crs.Tests.Unit.Api;

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
        userRepository.Setup(repo => repo.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetFeedRecommendationsAsync(Guid.NewGuid(), ContentType.Video, DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None));
    }

    [TestMethod]
    public async Task GetFeedRecommendationsAsync_WhenFallbackUsed_ReturnsMostRecent()
    {
        var service = CreateService(out var recommendationRepository, out var userRepository);
        var userId = Guid.NewGuid();
        userRepository.Setup(repo => repo.ExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var requestedDate = new DateOnly(2024, 12, 1);
        var fallbackDate = new DateOnly(2024, 11, 30);

        recommendationRepository.Setup(repo => repo.GetByUserDateAndTypeAsync(userId, requestedDate, ContentType.Paper, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RecommendationEntity>());
        recommendationRepository.Setup(repo => repo.GetMostRecentDateWithRecommendationsAsync(userId, ContentType.Paper, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackDate);

        var recommendations = new List<RecommendationEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Position = 2,
                FeedType = ContentType.Paper,
                Date = fallbackDate,
                Content = new Paper { Id = Guid.NewGuid(), Title = "Two", Url = "https://example.com/2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Position = 1,
                FeedType = ContentType.Paper,
                Date = fallbackDate,
                Content = new Paper { Id = Guid.NewGuid(), Title = "One", Url = "https://example.com/1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            }
        };

        recommendationRepository.Setup(repo => repo.GetByUserDateAndTypeAsync(userId, fallbackDate, ContentType.Paper, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recommendations);

        var response = await service.GetFeedRecommendationsAsync(userId, ContentType.Paper, requestedDate, CancellationToken.None);

        Assert.AreEqual(fallbackDate, response.Date);
        Assert.HasCount(2, response.Recommendations);
        Assert.AreEqual(1, response.Recommendations[0].Position);
    }

    [TestMethod]
    public async Task GetTodaysRecommendationsAsync_WhenUserMissing_Throws()
    {
        var service = CreateService(out _, out var userRepository);
        userRepository.Setup(repo => repo.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetTodaysRecommendationsAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [TestMethod]
    public async Task GetTodaysRecommendationsAsync_ReturnsOnlyFeedsWithData()
    {
        var service = CreateService(out var recommendationRepository, out var userRepository);
        var userId = Guid.NewGuid();
        userRepository.Setup(repo => repo.ExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        recommendationRepository.Setup(repo => repo.GetByUserDateAndTypeAsync(userId, today, It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RecommendationEntity>());
        recommendationRepository.Setup(repo => repo.GetMostRecentDateWithRecommendationsAsync(userId, It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly?)null);

        var recommendation = new RecommendationEntity
        {
            Id = Guid.NewGuid(),
            Position = 1,
            FeedType = ContentType.Video,
            Date = today,
            Content = new Video { Id = Guid.NewGuid(), Title = "Video", Url = "https://example.com/video", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        recommendationRepository.Setup(repo => repo.GetByUserDateAndTypeAsync(userId, today, ContentType.Video, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { recommendation });

        var result = await service.GetTodaysRecommendationsAsync(userId, CancellationToken.None);

        Assert.HasCount(1, result);
        Assert.AreEqual(ContentType.Video, result[0].FeedType);
    }
}
