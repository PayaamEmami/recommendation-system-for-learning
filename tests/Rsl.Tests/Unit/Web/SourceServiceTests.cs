using System.Net;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Core.Enums;
using Rsl.Tests.Unit.Api;
using Rsl.Web.Services;

namespace Rsl.Tests.Unit.Web;

[TestClass]
public sealed class SourceServiceTests
{
    [TestMethod]
    public async Task AddSourceAsync_WhenSuccess_ReturnsTrue()
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

        var handler = new HttpTestHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var service = new SourceService(new HttpClient(handler) { BaseAddress = new Uri("https://example.com") }, authService, NullLogger<SourceService>.Instance);

        var result = await service.AddSourceAsync("Test", "https://example.com", ResourceType.Video, null);

        Assert.IsTrue(result);
        Assert.HasCount(1, handler.Requests);
    }

    [TestMethod]
    public async Task BulkImportSourcesAsync_WhenUnauthenticated_Throws()
    {
        var localStorage = new Mock<ILocalStorageService>(MockBehavior.Strict);
        localStorage.Setup(store => store.GetItemAsync<AuthState>(It.IsAny<string>()))
            .ReturnsAsync((AuthState?)null);

        var authService = CreateAuthService(localStorage);

        var handler = new HttpTestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = new SourceService(new HttpClient(handler) { BaseAddress = new Uri("https://example.com") }, authService, NullLogger<SourceService>.Instance);

        await TestAssert.ThrowsAsync<InvalidOperationException>(() =>
            service.BulkImportSourcesAsync(JsonSerializer.Serialize(new { sources = Array.Empty<object>() })));
    }

    private static AuthService CreateAuthService(Mock<ILocalStorageService> localStorage)
    {
        var configuration = new ConfigurationBuilder().Build();
        var authHandler = new HttpTestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var authHttpClient = new HttpClient(authHandler) { BaseAddress = new Uri("https://example.com") };

        return new AuthService(authHttpClient, localStorage.Object, configuration, NullLogger<AuthService>.Instance);
    }
}
