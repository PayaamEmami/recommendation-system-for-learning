using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;
using Rsl.Core.Models;
using Rsl.Jobs.Jobs;
using Rsl.Llm.Models;
using Rsl.Llm.Services;

namespace Rsl.Tests.Unit.Jobs;

[TestClass]
public sealed class SourceIngestionJobTests
{
    [TestMethod]
    public async Task ExecuteAsync_WhenNoActiveSources_ReturnsEarly()
    {
        var sourceRepository = new Mock<ISourceRepository>(MockBehavior.Strict);
        var resourceRepository = new Mock<IResourceRepository>(MockBehavior.Strict);
        var ingestionAgent = new Mock<IIngestionAgent>(MockBehavior.Strict);
        var embeddingService = new Mock<IEmbeddingService>(MockBehavior.Strict);
        var vectorStore = new Mock<IVectorStore>(MockBehavior.Strict);

        sourceRepository.Setup(repo => repo.GetActiveSourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Source>());

        var provider = BuildProvider(
            sourceRepository.Object,
            resourceRepository.Object,
            ingestionAgent.Object,
            embeddingService.Object,
            vectorStore.Object);

        var job = new SourceIngestionJob(provider, NullLogger<SourceIngestionJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);

        ingestionAgent.VerifyNoOtherCalls();
        resourceRepository.VerifyNoOtherCalls();
        vectorStore.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenIngestionFails_SkipsResourceSave()
    {
        var sourceRepository = new Mock<ISourceRepository>(MockBehavior.Strict);
        var resourceRepository = new Mock<IResourceRepository>(MockBehavior.Strict);
        var ingestionAgent = new Mock<IIngestionAgent>(MockBehavior.Strict);
        var embeddingService = new Mock<IEmbeddingService>(MockBehavior.Strict);
        var vectorStore = new Mock<IVectorStore>(MockBehavior.Strict);

        var source = new Source { Id = Guid.NewGuid(), Name = "Test", Url = "https://example.com" };
        sourceRepository.Setup(repo => repo.GetActiveSourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Source> { source });

        ingestionAgent.Setup(agent => agent.IngestFromUrlAsync(source.Url, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestionResult { Success = false, ErrorMessage = "failed" });

        var provider = BuildProvider(
            sourceRepository.Object,
            resourceRepository.Object,
            ingestionAgent.Object,
            embeddingService.Object,
            vectorStore.Object);

        var job = new SourceIngestionJob(provider, NullLogger<SourceIngestionJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);

        resourceRepository.Verify(repo => repo.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Never);
        vectorStore.Verify(store => store.UpsertDocumentsAsync(It.IsAny<IEnumerable<ResourceDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ServiceProvider BuildProvider(
        ISourceRepository sourceRepository,
        IResourceRepository resourceRepository,
        IIngestionAgent ingestionAgent,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sourceRepository);
        services.AddSingleton(resourceRepository);
        services.AddSingleton(ingestionAgent);
        services.AddSingleton(embeddingService);
        services.AddSingleton(vectorStore);
        return services.BuildServiceProvider();
    }
}
