using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Api.Controllers;
using Rsl.Api.DTOs.Common;
using Rsl.Api.DTOs.Resources.Responses;
using Rsl.Api.Services;
using Rsl.Core.Enums;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class ResourcesControllerTests
{
    private static ResourcesController CreateController(
        out Mock<IResourceService> resourceService,
        out Mock<IVoteService> voteService)
    {
        resourceService = new Mock<IResourceService>(MockBehavior.Strict);
        voteService = new Mock<IVoteService>(MockBehavior.Strict);

        return new ResourcesController(
            resourceService.Object,
            voteService.Object,
            NullLogger<ResourcesController>.Instance);
    }

    [TestMethod]
    public async Task GetResources_WhenPageNumberInvalid_ReturnsBadRequest()
    {
        var controller = CreateController(out _, out _);

        var result = await controller.GetResources(0, 20, null, null, CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("Page number must be greater than 0", badRequest.Value);
    }

    [TestMethod]
    public async Task GetResources_WhenPageSizeInvalid_ReturnsBadRequest()
    {
        var controller = CreateController(out _, out _);

        var result = await controller.GetResources(1, 101, null, null, CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("Page size must be between 1 and 100", badRequest.Value);
    }

    [TestMethod]
    public async Task GetResources_WhenTopicIdsInvalid_ReturnsBadRequest()
    {
        var controller = CreateController(out _, out _);

        var result = await controller.GetResources(1, 20, null, "not-a-guid", CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("Invalid topic IDs format", badRequest.Value);
    }

    [TestMethod]
    public async Task GetResources_WhenValidRequest_ReturnsOkWithResponse()
    {
        var controller = CreateController(out var resourceService, out _);
        var expectedIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var expectedResponse = new PagedResponse<ResourceResponse>
        {
            Items = new List<ResourceResponse>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Resource",
                    Url = "https://example.com",
                    Type = ResourceType.BlogPost,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            },
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };

        resourceService
            .Setup(service => service.GetResourcesAsync(
                1,
                20,
                ResourceType.BlogPost,
                It.Is<List<Guid>>(ids => ids.SequenceEqual(expectedIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await controller.GetResources(
            1,
            20,
            ResourceType.BlogPost,
            $"{expectedIds[0]},{expectedIds[1]}",
            CancellationToken.None);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreSame(expectedResponse, okResult.Value);
    }

    [TestMethod]
    public async Task GetResourceById_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(out var resourceService, out _);
        var resourceId = Guid.NewGuid();

        resourceService
            .Setup(service => service.GetResourceByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceResponse?)null);

        var result = await controller.GetResourceById(resourceId, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task GetResourceById_WhenFound_ReturnsOk()
    {
        var controller = CreateController(out var resourceService, out _);
        var resourceId = Guid.NewGuid();
        var response = new ResourceResponse
        {
            Id = resourceId,
            Title = "Existing Resource",
            Url = "https://example.com/resource",
            Type = ResourceType.Video,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        resourceService
            .Setup(service => service.GetResourceByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.GetResourceById(resourceId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreSame(response, okResult.Value);
    }
}
