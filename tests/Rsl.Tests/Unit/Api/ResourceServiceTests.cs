using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Api.DTOs.Resources.Requests;
using Rsl.Api.Services;
using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class ResourceServiceTests
{
    private static ResourceService CreateService(
        out Mock<IResourceRepository> resourceRepository,
        out Mock<ISourceRepository> sourceRepository)
    {
        resourceRepository = new Mock<IResourceRepository>(MockBehavior.Strict);
        sourceRepository = new Mock<ISourceRepository>(MockBehavior.Strict);
        return new ResourceService(resourceRepository.Object, sourceRepository.Object, NullLogger<ResourceService>.Instance);
    }

    [TestMethod]
    public async Task GetResourcesAsync_WhenTypeProvided_OrdersAndPaginates()
    {
        var service = CreateService(out var resourceRepository, out _);
        var newest = new Video { Id = Guid.NewGuid(), Title = "New", CreatedAt = DateTime.UtcNow.AddDays(1) };
        var older = new Video { Id = Guid.NewGuid(), Title = "Old", CreatedAt = DateTime.UtcNow.AddDays(-1) };

        resourceRepository.Setup(repo => repo.GetByTypeAsync(ResourceType.Video, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resource> { older, newest });

        var response = await service.GetResourcesAsync(1, 1, ResourceType.Video, null, CancellationToken.None);

        Assert.AreEqual(2, response.TotalCount);
        Assert.HasCount(1, response.Items);
        Assert.AreEqual(newest.Id, response.Items[0].Id);
    }

    [TestMethod]
    public async Task GetResourcesAsync_WhenSourceFilterApplied_ReturnsFiltered()
    {
        var service = CreateService(out var resourceRepository, out _);
        var sourceId = Guid.NewGuid();

        resourceRepository.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resource>
            {
                new BlogPost
                {
                    Id = Guid.NewGuid(),
                    SourceId = sourceId,
                    Source = new Source { Id = sourceId },
                    CreatedAt = DateTime.UtcNow
                },
                new BlogPost { Id = Guid.NewGuid(), SourceId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
            });

        var response = await service.GetResourcesAsync(1, 10, null, new List<Guid> { sourceId }, CancellationToken.None);

        Assert.AreEqual(1, response.TotalCount);
        Assert.HasCount(1, response.Items);
        Assert.AreEqual(sourceId, response.Items[0].SourceInfo?.Id);
    }

    [TestMethod]
    public async Task CreateResourceAsync_WhenSourceMissing_Throws()
    {
        var service = CreateService(out _, out var sourceRepository);
        var sourceId = Guid.NewGuid();
        sourceRepository.Setup(repo => repo.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Source?)null);

        var request = new CreateResourceRequest
        {
            Title = "Test",
            Url = "https://example.com",
            ResourceType = ResourceType.Video,
            SourceId = sourceId
        };

        await TestAssert.ThrowsAsync<ArgumentException>(() =>
            service.CreateResourceAsync(request, CancellationToken.None));
    }

    [TestMethod]
    public async Task CreateResourceAsync_WhenTypeInvalid_Throws()
    {
        var service = CreateService(out _, out var sourceRepository);
        sourceRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Source { Id = Guid.NewGuid() });

        var request = new CreateResourceRequest
        {
            Title = "Test",
            Url = "https://example.com",
            ResourceType = (ResourceType)99
        };

        await TestAssert.ThrowsAsync<ArgumentException>(() =>
            service.CreateResourceAsync(request, CancellationToken.None));
    }

    [TestMethod]
    public async Task CreateResourceAsync_WhenValid_CreatesResource()
    {
        var service = CreateService(out var resourceRepository, out _);
        resourceRepository.Setup(repo => repo.CreateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource resource, CancellationToken _) => resource);

        var response = await service.CreateResourceAsync(new CreateResourceRequest
        {
            Title = "Title",
            Url = "https://example.com",
            ResourceType = ResourceType.Paper
        }, CancellationToken.None);

        Assert.AreEqual("Title", response.Title);
        Assert.AreEqual(ResourceType.Paper, response.Type);
        resourceRepository.Verify(repo => repo.CreateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateResourceAsync_WhenMissing_Throws()
    {
        var service = CreateService(out var resourceRepository, out _);
        resourceRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateResourceAsync(Guid.NewGuid(), new UpdateResourceRequest(), CancellationToken.None));
    }

    [TestMethod]
    public async Task UpdateResourceAsync_WhenSourceMissing_Throws()
    {
        var service = CreateService(out var resourceRepository, out var sourceRepository);
        var resource = new Paper { Id = Guid.NewGuid(), Title = "Old" };
        resourceRepository.Setup(repo => repo.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);
        sourceRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Source?)null);

        await TestAssert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateResourceAsync(resource.Id, new UpdateResourceRequest { SourceId = Guid.NewGuid() }, CancellationToken.None));
    }

    [TestMethod]
    public async Task UpdateResourceAsync_WhenValid_UpdatesAndReturns()
    {
        var service = CreateService(out var resourceRepository, out var sourceRepository);
        var resource = new Paper { Id = Guid.NewGuid(), Title = "Old", Url = "https://old.com" };
        resourceRepository.Setup(repo => repo.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);
        sourceRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Source { Id = Guid.NewGuid() });
        resourceRepository.Setup(repo => repo.UpdateAsync(resource, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource resourceToUpdate, CancellationToken _) => resourceToUpdate);

        var response = await service.UpdateResourceAsync(resource.Id, new UpdateResourceRequest
        {
            Title = "New",
            Url = "https://new.com",
            SourceId = Guid.NewGuid()
        }, CancellationToken.None);

        Assert.AreEqual("New", response.Title);
        Assert.AreEqual("https://new.com", response.Url);
    }

    [TestMethod]
    public async Task DeleteResourceAsync_WhenMissing_Throws()
    {
        var service = CreateService(out var resourceRepository, out _);
        resourceRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        await TestAssert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DeleteResourceAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [TestMethod]
    public async Task DeleteResourceAsync_WhenExists_Deletes()
    {
        var service = CreateService(out var resourceRepository, out _);
        var resourceId = Guid.NewGuid();
        resourceRepository.Setup(repo => repo.GetByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogPost { Id = resourceId });
        resourceRepository.Setup(repo => repo.DeleteAsync(resourceId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await service.DeleteResourceAsync(resourceId, CancellationToken.None);

        resourceRepository.Verify(repo => repo.DeleteAsync(resourceId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
