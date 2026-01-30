using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;
using Rsl.Core.Models;
using Rsl.Jobs.Jobs;

namespace Rsl.Tests.Unit.Jobs;

[TestClass]
public sealed class ReindexJobTests
{
    [TestMethod]
    public async Task ExecuteAsync_WhenNoResources_ReturnsEarly()
    {
        var resourceRepository = new Mock<IResourceRepository>(MockBehavior.Strict);
        var embeddingService = new Mock<IEmbeddingService>(MockBehavior.Strict);
        var vectorStore = new Mock<IVectorStore>(MockBehavior.Strict);

        resourceRepository.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Resource>());

        var provider = BuildProvider(resourceRepository.Object, embeddingService.Object, vectorStore.Object);
        var job = new ReindexJob(provider, NullLogger<ReindexJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);

        vectorStore.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenResourcesExist_UpsertsDocuments()
    {
        var resourceRepository = new Mock<IResourceRepository>(MockBehavior.Strict);
        var embeddingService = new Mock<IEmbeddingService>(MockBehavior.Strict);
        var vectorStore = new Mock<IVectorStore>(MockBehavior.Strict);

        var resources = new List<Resource>
        {
            new BlogPost { Id = Guid.NewGuid(), Title = "A", Url = "https://example.com/a", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Video { Id = Guid.NewGuid(), Title = "B", Url = "https://example.com/b", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        resourceRepository.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resources);
        embeddingService.Setup(service => service.GenerateEmbeddingsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { new[] { 0.1f }, new[] { 0.2f } });
        vectorStore.Setup(store => store.UpsertDocumentsAsync(It.IsAny<IEnumerable<ResourceDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var provider = BuildProvider(resourceRepository.Object, embeddingService.Object, vectorStore.Object);
        var job = new ReindexJob(provider, NullLogger<ReindexJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);

        vectorStore.Verify(store => store.UpsertDocumentsAsync(
            It.Is<IEnumerable<ResourceDocument>>(docs => docs.Count() == resources.Count),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    private static ServiceProvider BuildProvider(
        IResourceRepository resourceRepository,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore)
    {
        var services = new ServiceCollection();
        services.AddSingleton(resourceRepository);
        services.AddSingleton(embeddingService);
        services.AddSingleton(vectorStore);
        return services.BuildServiceProvider();
    }
}
