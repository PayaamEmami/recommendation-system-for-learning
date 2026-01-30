using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Api.Controllers;
using Rsl.Api.DTOs.Ingestion.Requests;
using Rsl.Api.DTOs.Resources.Requests;
using Rsl.Api.DTOs.Resources.Responses;
using Rsl.Api.DTOs.Sources.Responses;
using Rsl.Api.Services;
using Rsl.Core.Enums;
using Rsl.Llm.Models;
using Rsl.Llm.Services;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class IngestionControllerTests
{
    private static IngestionController CreateController(
        out Mock<IIngestionAgent> ingestionAgent,
        out Mock<ISourceService> sourceService,
        out Mock<IResourceService> resourceService)
    {
        ingestionAgent = new Mock<IIngestionAgent>(MockBehavior.Strict);
        sourceService = new Mock<ISourceService>(MockBehavior.Strict);
        resourceService = new Mock<IResourceService>(MockBehavior.Strict);
        return new IngestionController(
            ingestionAgent.Object,
            sourceService.Object,
            resourceService.Object,
            NullLogger<IngestionController>.Instance);
    }

    [TestMethod]
    public async Task IngestFromUrl_WhenInvalidUrl_ReturnsBadRequest()
    {
        var controller = CreateController(out _, out _, out _);

        var result = await controller.IngestFromUrl(new IngestUrlRequest { Url = "not-a-url" }, CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("Invalid URL provided.", GetProperty<string?>(badRequest.Value!, "message"));
    }

    [TestMethod]
    public async Task IngestFromUrl_WhenIngestionFails_ReturnsServerError()
    {
        var controller = CreateController(out var ingestionAgent, out _, out _);

        ingestionAgent.Setup(agent => agent.IngestFromUrlAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestionResult
            {
                Success = false,
                ErrorMessage = "LLM error",
                TotalFound = 1,
                NewResources = 0,
                DuplicatesSkipped = 1
            });

        var result = await controller.IngestFromUrl(new IngestUrlRequest { Url = "https://example.com" }, CancellationToken.None);

        var objectResult = result as ObjectResult;
        Assert.IsNotNull(objectResult);
        Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.AreEqual("Ingestion failed", GetProperty<string?>(objectResult.Value!, "message"));
    }

    [TestMethod]
    public async Task IngestFromUrl_WhenSuccess_ReturnsOk()
    {
        var controller = CreateController(out var ingestionAgent, out _, out _);
        var ingestionResult = new IngestionResult
        {
            Success = true,
            TotalFound = 2,
            NewResources = 2,
            DuplicatesSkipped = 0,
            Resources = new List<ExtractedResource>
            {
                new() { Title = "One", Url = "https://example.com/1", Description = "Desc", Type = ResourceType.Paper }
            }
        };

        ingestionAgent.Setup(agent => agent.IngestFromUrlAsync("https://example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingestionResult);

        var result = await controller.IngestFromUrl(new IngestUrlRequest { Url = "https://example.com" }, CancellationToken.None);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.IsTrue(GetProperty<bool>(okResult.Value!, "success"));
        Assert.AreEqual(2, GetProperty<int>(okResult.Value!, "totalFound"));
    }

    [TestMethod]
    public async Task IngestFromSource_WhenMissingUser_ReturnsUnauthorized()
    {
        var controller = CreateController(out _, out _, out _);
        ControllerTestHelpers.SetUser(controller, Guid.Empty);

        var result = await controller.IngestFromSource(Guid.NewGuid(), CancellationToken.None);

        Assert.IsInstanceOfType<UnauthorizedObjectResult>(result);
    }

    [TestMethod]
    public async Task IngestFromSource_WhenSourceMissing_ReturnsNotFound()
    {
        var controller = CreateController(out _, out var sourceService, out _);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);
        var sourceId = Guid.NewGuid();

        sourceService.Setup(service => service.GetSourceByIdAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceResponse?)null);

        var result = await controller.IngestFromSource(sourceId, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task IngestFromSource_WhenDifferentOwner_ReturnsForbid()
    {
        var controller = CreateController(out _, out var sourceService, out _);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);
        var sourceId = Guid.NewGuid();

        sourceService.Setup(service => service.GetSourceByIdAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SourceResponse
            {
                Id = sourceId,
                UserId = Guid.NewGuid(),
                Url = "https://example.com"
            });

        var result = await controller.IngestFromSource(sourceId, CancellationToken.None);

        Assert.IsInstanceOfType<ForbidResult>(result);
    }

    [TestMethod]
    public async Task IngestFromSource_WhenSuccess_ReturnsOk()
    {
        var controller = CreateController(out var ingestionAgent, out var sourceService, out var resourceService);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);
        var sourceId = Guid.NewGuid();

        sourceService.Setup(service => service.GetSourceByIdAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SourceResponse
            {
                Id = sourceId,
                UserId = userId,
                Url = "https://example.com"
            });

        ingestionAgent.Setup(agent => agent.IngestFromUrlAsync("https://example.com", sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestionResult
            {
                Success = true,
                TotalFound = 1,
                NewResources = 1,
                DuplicatesSkipped = 0,
                Resources = new List<ExtractedResource>
                {
                    new() { Title = "One", Url = "https://example.com/1", Description = "Desc", Type = ResourceType.Video }
                }
            });

        resourceService.Setup(service => service.CreateResourceAsync(It.IsAny<CreateResourceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceResponse
            {
                Id = Guid.NewGuid(),
                Title = "One",
                Url = "https://example.com/1",
                Type = ResourceType.Video,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        var result = await controller.IngestFromSource(sourceId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.IsTrue(GetProperty<bool>(okResult.Value!, "success"));
        Assert.AreEqual(1, GetProperty<int>(okResult.Value!, "savedCount"));
    }

    private static T? GetProperty<T>(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name);
        return property == null ? default : (T?)property.GetValue(instance);
    }
}
