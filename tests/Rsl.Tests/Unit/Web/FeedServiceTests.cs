using System.Net;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Core.Enums;
using Rsl.Web.Services;

namespace Rsl.Tests.Unit.Web;

[TestClass]
public sealed class FeedServiceTests
{
    [TestMethod]
    public async Task GetFeedAsync_WhenUnauthenticated_ReturnsEmptyAndNoRequests()
    {
        var localStorage = new Mock<ILocalStorageService>(MockBehavior.Strict);
        localStorage.Setup(store => store.GetItemAsync<AuthState>(It.IsAny<string>()))
            .ReturnsAsync((AuthState?)null);

        var authService = CreateAuthService(localStorage);
        var handler = new HttpTestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = new FeedService(new HttpClient(handler) { BaseAddress = new Uri("https://example.com") }, authService, NullLogger<FeedService>.Instance);

        var result = await service.GetFeedAsync();

        Assert.HasCount(0, result);
        Assert.HasCount(0, handler.Requests);
    }

    [TestMethod]
    public async Task GetFeedAsync_WhenSuccess_ReturnsSortedResources()
    {
        var localStorage = new Mock<ILocalStorageService>(MockBehavior.Strict);
        localStorage.Setup(store => store.GetItemAsync<AuthState>(It.IsAny<string>()))
            .ReturnsAsync(new AuthState
            {
                IsAuthenticated = true,
                AccessToken = "access",
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            });

        var authService = CreateAuthService(localStorage);
        await authService.InitializeAsync();

        var payload = new List<FeedRecommendationsResponse>
        {
            new()
            {
                FeedType = ResourceType.Video,
                Recommendations = new List<RecommendationItemResponse>
                {
                    new()
                    {
                        Resource = new ResourceItemResponse
                        {
                            Id = Guid.NewGuid(),
                            Title = "Older",
                            Url = "https://example.com/old",
                            Type = ResourceType.Video,
                            CreatedAt = DateTime.UtcNow.AddDays(-2),
                            PublishedDate = DateTime.UtcNow.AddDays(-2)
                        }
                    },
                    new()
                    {
                        Resource = new ResourceItemResponse
                        {
                            Id = Guid.NewGuid(),
                            Title = "Newer",
                            Url = "https://example.com/new",
                            Type = ResourceType.Video,
                            CreatedAt = DateTime.UtcNow.AddDays(-1),
                            PublishedDate = DateTime.UtcNow.AddDays(-1)
                        }
                    }
                }
            }
        };

        var handler = new HttpTestHandler(_ =>
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
        });

        var service = new FeedService(new HttpClient(handler) { BaseAddress = new Uri("https://example.com") }, authService, NullLogger<FeedService>.Instance);

        var result = await service.GetFeedAsync();

        Assert.HasCount(2, result);
        Assert.AreEqual("Newer", result[0].Title);
    }

    private static AuthService CreateAuthService(Mock<ILocalStorageService> localStorage)
    {
        var configuration = new ConfigurationBuilder().Build();
        var authHandler = new HttpTestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var authHttpClient = new HttpClient(authHandler) { BaseAddress = new Uri("https://example.com") };

        return new AuthService(authHttpClient, localStorage.Object, configuration, NullLogger<AuthService>.Instance);
    }
}
