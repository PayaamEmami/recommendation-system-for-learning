using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Crs.Core.Entities;
using Crs.Core.Interfaces;
using Crs.Core.Models;
using Crs.Jobs.Jobs;

namespace Crs.Tests.Unit.Jobs;

[TestClass]
public sealed class ReindexJobTests
{
    [TestMethod]
    public async Task ExecuteAsync_WhenNoContent_ReturnsEarly()
    {
        var contentRepository = new Mock<IContentRepository>(MockBehavior.Strict);
        var embeddingService = new Mock<IEmbeddingService>(MockBehavior.Strict);
        var vectorStore = new Mock<IVectorStore>(MockBehavior.Strict);

        contentRepository.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Content>());

        var provider = BuildProvider(contentRepository.Object, embeddingService.Object, vectorStore.Object);
        var job = new ReindexJob(provider, NullLogger<ReindexJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);

        vectorStore.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenContentExist_UpsertsDocuments()
    {
        var contentRepository = new Mock<IContentRepository>(MockBehavior.Strict);
        var embeddingService = new Mock<IEmbeddingService>(MockBehavior.Strict);
        var vectorStore = new Mock<IVectorStore>(MockBehavior.Strict);

        var content = new List<Content>
        {
            new BlogPost { Id = Guid.NewGuid(), Title = "A", Url = "https://example.com/a", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Video { Id = Guid.NewGuid(), Title = "B", Url = "https://example.com/b", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        contentRepository.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
        embeddingService.Setup(service => service.GenerateEmbeddingsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { new[] { 0.1f }, new[] { 0.2f } });
        vectorStore.Setup(store => store.UpsertDocumentsAsync(It.IsAny<IEnumerable<ContentDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var provider = BuildProvider(contentRepository.Object, embeddingService.Object, vectorStore.Object);
        var job = new ReindexJob(provider, NullLogger<ReindexJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);

        vectorStore.Verify(store => store.UpsertDocumentsAsync(
            It.Is<IEnumerable<ContentDocument>>(docs => docs.Count() == content.Count),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    private static ServiceProvider BuildProvider(
        IContentRepository contentRepository,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore)
    {
        var services = new ServiceCollection();
        services.AddSingleton(contentRepository);
        services.AddSingleton(embeddingService);
        services.AddSingleton(vectorStore);
        return services.BuildServiceProvider();
    }
}
