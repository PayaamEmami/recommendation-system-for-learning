using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Api.Controllers;
using Rsl.Api.DTOs.Recommendations.Responses;
using Rsl.Api.Services;
using Rsl.Core.Enums;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class RecommendationsControllerTests
{
    private static RecommendationsController CreateController(out Mock<IRecommendationService> service)
    {
        service = new Mock<IRecommendationService>(MockBehavior.Strict);
        return new RecommendationsController(service.Object, NullLogger<RecommendationsController>.Instance);
    }

    [TestMethod]
    public async Task GetTodaysRecommendations_WhenMissingUser_ReturnsUnauthorized()
    {
        var controller = CreateController(out _);
        ControllerTestHelpers.SetUser(controller, null);

        var result = await controller.GetTodaysRecommendations(CancellationToken.None);

        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    [TestMethod]
    public async Task GetTodaysRecommendations_ReturnsOk()
    {
        var controller = CreateController(out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        var response = new List<FeedRecommendationsResponse>();
        service.Setup(svc => svc.GetTodaysRecommendationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.GetTodaysRecommendations(CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreSame(response, ok.Value);
    }

    [TestMethod]
    public async Task GetFeedRecommendations_WhenMissingUser_ReturnsUnauthorized()
    {
        var controller = CreateController(out _);
        ControllerTestHelpers.SetUser(controller, null);

        var result = await controller.GetFeedRecommendations(ResourceType.Video, null, CancellationToken.None);

        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    [TestMethod]
    public async Task GetFeedRecommendations_ReturnsOk()
    {
        var controller = CreateController(out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        var response = new FeedRecommendationsResponse { FeedType = ResourceType.Video, Date = DateOnly.FromDateTime(DateTime.UtcNow) };
        service.Setup(svc => svc.GetFeedRecommendationsAsync(userId, ResourceType.Video, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.GetFeedRecommendations(ResourceType.Video, null, CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreSame(response, ok.Value);
    }
}
