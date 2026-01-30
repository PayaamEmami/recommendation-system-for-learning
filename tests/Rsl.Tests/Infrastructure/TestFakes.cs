using Rsl.Core.Interfaces;
using Rsl.Core.Models;
using Rsl.Llm.Models;
using Rsl.Llm.Services;

namespace Rsl.Tests.Infrastructure;

public sealed class FakeEmbeddingService : IEmbeddingService
{
    public int Dimensions => 3;

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { 0.1f, 0.2f, 0.3f });
    }

    public Task<IList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        IList<float[]> results = texts.Select(_ => new[] { 0.1f, 0.2f, 0.3f }).ToList();
        return Task.FromResult(results);
    }
}

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly Dictionary<Guid, ResourceDocument> _documents = new();

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UpsertDocumentAsync(ResourceDocument document, CancellationToken cancellationToken = default)
    {
        _documents[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task UpsertDocumentsAsync(IEnumerable<ResourceDocument> documents, CancellationToken cancellationToken = default)
    {
        foreach (var document in documents)
        {
            _documents[document.Id] = document;
        }

        return Task.CompletedTask;
    }

    public Task DeleteDocumentAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        _documents.Remove(resourceId);
        return Task.CompletedTask;
    }

    public Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<VectorSearchResult>());
    }

    public Task<long> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_documents.Count);
    }
}

public sealed class FakeLlmClient : ILlmClient
{
    public Task<LlmResponse> SendMessageAsync(
        string systemPrompt,
        string userMessage,
        List<object>? tools = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LlmResponse { Content = string.Empty, IsComplete = true });
    }

    public Task<LlmResponse> ContinueConversationAsync(
        List<object> conversationHistory,
        List<ToolResult> toolResults,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LlmResponse { Content = string.Empty, IsComplete = true });
    }
}

public sealed class FakeIngestionAgent : IIngestionAgent
{
    public Task<IngestionResult> IngestFromUrlAsync(
        string sourceUrl,
        Guid? sourceId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new IngestionResult
        {
            Success = true,
            SourceUrl = sourceUrl,
            Resources = new List<ExtractedResource>(),
            TotalFound = 0,
            NewResources = 0,
            DuplicatesSkipped = 0
        });
    }
}

public sealed class FakeXApiClient : IXApiClient
{
    public Task<XTokenResponse> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new XTokenResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresIn = 3600,
            Scope = "users.read",
            TokenType = "bearer"
        });
    }

    public Task<XTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new XTokenResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresIn = 3600,
            Scope = "users.read",
            TokenType = "bearer"
        });
    }

    public Task<XUserProfile> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new XUserProfile
        {
            XUserId = "x-user",
            Handle = "testuser",
            DisplayName = "Test User"
        });
    }

    public Task<List<XFollowedAccountInfo>> GetFollowedAccountsAsync(
        string accessToken,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<XFollowedAccountInfo>());
    }

    public Task<List<XPostInfo>> GetRecentPostsAsync(
        string accessToken,
        string userId,
        DateTime? since,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<XPostInfo>());
    }
}

public sealed class FakeContentFetcherService : IContentFetcherService
{
    public Task<ContentFetchResult> FetchContentAsync(string url, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ContentFetchResult
        {
            Success = true,
            Content = string.Empty,
            StatusCode = 200
        });
    }
}
