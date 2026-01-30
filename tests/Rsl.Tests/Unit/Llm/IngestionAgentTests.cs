using Microsoft.Extensions.Logging;
using Moq;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Llm.Models;
using Rsl.Llm.Services;

namespace Rsl.Tests.Unit.Llm;

[TestClass]
public class IngestionAgentTests
{
    private Mock<ILlmClient> _mockLlmClient = null!;
    private Mock<IContentFetcherService> _mockContentFetcher = null!;
    private Mock<ILogger<IngestionAgent>> _mockLogger = null!;
    private IngestionAgent _agent = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new Mock<ILlmClient>();
        _mockContentFetcher = new Mock<IContentFetcherService>();
        _mockLogger = new Mock<ILogger<IngestionAgent>>();
        _agent = new IngestionAgent(_mockLlmClient.Object, _mockContentFetcher.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_SuccessfulIngestion_ReturnsResources()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        var jsonResponse = @"{
            ""resources"": [
                {
                    ""title"": ""Test Paper"",
                    ""url"": ""https://example.com/paper1"",
                    ""description"": ""A test paper about AI"",
                    ""type"": ""Paper""
                },
                {
                    ""title"": ""Test Video"",
                    ""url"": ""https://example.com/video1"",
                    ""description"": ""A tutorial video"",
                    ""type"": ""Video""
                }
            ]
        }";

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = jsonResponse,
                FinishReason = "stop",
                CompletionTokens = 100
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(sourceUrl, result.SourceUrl);
        Assert.HasCount(2, result.Resources);
        Assert.AreEqual(2, result.TotalFound);

        var paper = result.Resources[0];
        Assert.AreEqual("Test Paper", paper.Title);
        Assert.AreEqual("https://example.com/paper1", paper.Url);
        Assert.AreEqual("A test paper about AI", paper.Description);
        Assert.AreEqual(ResourceType.Paper, paper.Type);

        var video = result.Resources[1];
        Assert.AreEqual("Test Video", video.Title);
        Assert.AreEqual("https://example.com/video1", video.Url);
        Assert.AreEqual("A tutorial video", video.Description);
        Assert.AreEqual(ResourceType.Video, video.Type);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_ContentFetchFails_ReturnsEmptyResult()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult
            {
                Success = false,
                ErrorMessage = "Network error"
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success); // Still returns success, but with 0 resources
        Assert.AreEqual(sourceUrl, result.SourceUrl);
        Assert.IsEmpty(result.Resources);
        Assert.AreEqual(0, result.TotalFound);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("Network error") || result.ErrorMessage.Contains("Failed to fetch content"));
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_EmptyContent_ReturnsEmptyResult()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult
            {
                Success = true,
                Content = ""
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsEmpty(result.Resources);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_NoJsonInResponse_ReturnsEmptyResult()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = "No resources found in this content.",
                FinishReason = "stop",
                CompletionTokens = 50
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsEmpty(result.Resources);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_MalformedJson_ReturnsEmptyResult()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        // Malformed JSON (missing closing braces/brackets)
        var malformedJson = @"{
            ""resources"": [
                {
                    ""title"": ""Test Paper"",
                    ""url"": ""https://example.com/paper1""
                }
            ";

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = malformedJson,
                FinishReason = "length",
                CompletionTokens = 4096
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsEmpty(result.Resources);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_EmptyResourcesArray_ReturnsEmptyResult()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        var jsonResponse = @"{ ""resources"": [] }";

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = jsonResponse,
                FinishReason = "stop",
                CompletionTokens = 50
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsEmpty(result.Resources);
        Assert.AreEqual(0, result.TotalFound);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_ResourceMissingTitle_SkipsResource()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        var jsonResponse = @"{
            ""resources"": [
                {
                    ""url"": ""https://example.com/paper1"",
                    ""description"": ""A test paper without title"",
                    ""type"": ""Paper""
                },
                {
                    ""title"": ""Valid Paper"",
                    ""url"": ""https://example.com/paper2"",
                    ""description"": ""A valid paper"",
                    ""type"": ""Paper""
                }
            ]
        }";

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = jsonResponse,
                FinishReason = "stop",
                CompletionTokens = 100
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.HasCount(1, result.Resources);
        Assert.AreEqual("Valid Paper", result.Resources[0].Title);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_ResourceMissingUrl_SkipsResource()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        var jsonResponse = @"{
            ""resources"": [
                {
                    ""title"": ""Paper Without URL"",
                    ""description"": ""A test paper without URL"",
                    ""type"": ""Paper""
                },
                {
                    ""title"": ""Valid Paper"",
                    ""url"": ""https://example.com/paper2"",
                    ""description"": ""A valid paper"",
                    ""type"": ""Paper""
                }
            ]
        }";

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = jsonResponse,
                FinishReason = "stop",
                CompletionTokens = 100
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.HasCount(1, result.Resources);
        Assert.AreEqual("Valid Paper", result.Resources[0].Title);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_DefaultsToResourceTypePaper_WhenTypeNotSpecified()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        var jsonResponse = @"{
            ""resources"": [
                {
                    ""title"": ""Resource Without Type"",
                    ""url"": ""https://example.com/resource1"",
                    ""description"": ""A resource without type field""
                }
            ]
        }";

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = jsonResponse,
                FinishReason = "stop",
                CompletionTokens = 50
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.HasCount(1, result.Resources);
        Assert.AreEqual(ResourceType.Paper, result.Resources[0].Type);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_ParsesAllResourceTypes_Correctly()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        var jsonResponse = @"{
            ""resources"": [
                {
                    ""title"": ""Paper Resource"",
                    ""url"": ""https://example.com/paper"",
                    ""description"": ""A paper"",
                    ""type"": ""Paper""
                },
                {
                    ""title"": ""Video Resource"",
                    ""url"": ""https://example.com/video"",
                    ""description"": ""A video"",
                    ""type"": ""Video""
                },
                {
                    ""title"": ""BlogPost Resource"",
                    ""url"": ""https://example.com/blog"",
                    ""description"": ""A blog post"",
                    ""type"": ""BlogPost""
                }
            ]
        }";

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = jsonResponse,
                FinishReason = "stop",
                CompletionTokens = 150
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.HasCount(3, result.Resources);
        Assert.AreEqual(ResourceType.Paper, result.Resources[0].Type);
        Assert.AreEqual(ResourceType.Video, result.Resources[1].Type);
        Assert.AreEqual(ResourceType.BlogPost, result.Resources[2].Type);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_HandlesInvalidResourceType_DefaultsToPaper()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";
        var htmlContent = "<html><body>Some content</body></html>";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentFetchResult { Success = true, Content = htmlContent });

        var jsonResponse = @"{
            ""resources"": [
                {
                    ""title"": ""Resource With Invalid Type"",
                    ""url"": ""https://example.com/resource"",
                    ""description"": ""A resource with invalid type"",
                    ""type"": ""InvalidType""
                }
            ]
        }";

        _mockLlmClient
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = jsonResponse,
                FinishReason = "stop",
                CompletionTokens = 50
            });

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.HasCount(1, result.Resources);
        Assert.AreEqual(ResourceType.Paper, result.Resources[0].Type);
    }

    [TestMethod]
    public async Task IngestFromUrlAsync_HandlesException_ReturnsErrorResult()
    {
        // Arrange
        var sourceUrl = "https://example.com/feed";

        _mockContentFetcher
            .Setup(x => x.FetchContentAsync(sourceUrl, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _agent.IngestFromUrlAsync(sourceUrl);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsEmpty(result.Resources);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("Unexpected error", result.ErrorMessage);
    }
}
