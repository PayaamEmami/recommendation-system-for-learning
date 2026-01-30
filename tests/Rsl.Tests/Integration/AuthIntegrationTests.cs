using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Rsl.Api.DTOs.Auth.Requests;
using Rsl.Api.DTOs.Auth.Responses;
using Rsl.Tests.Infrastructure;

namespace Rsl.Tests.Integration;

[TestClass]
public sealed class AuthIntegrationTests
{
    private static ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _factory = new ApiWebApplicationFactory();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory.Dispose();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client.Dispose();
    }

    [TestMethod]
    public async Task RegistrationStatus_ReturnsEnabled()
    {
        var response = await _client.GetAsync("/api/v1/Auth/registration-status");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var enabled = document.RootElement.GetProperty("enabled").GetBoolean();
        Assert.IsTrue(enabled);
    }

    [TestMethod]
    public async Task Register_ReturnsTokensAndUser()
    {
        var request = new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123!",
            DisplayName = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(payload);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.AreEqual(request.Email, payload.User.Email);
    }

    [TestMethod]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var request = new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123!",
            DisplayName = "Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/Auth/register", request);
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginRequest
        {
            Email = request.Email,
            Password = request.Password
        });

        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);

        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(payload);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.AccessToken));
    }

    [TestMethod]
    public async Task Refresh_ReturnsNewTokens()
    {
        var request = new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123!",
            DisplayName = "Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/Auth/register", request);
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(registerPayload);

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/Auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = registerPayload.RefreshToken
        });

        Assert.AreEqual(HttpStatusCode.OK, refreshResponse.StatusCode);

        var payload = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        Assert.IsNotNull(payload);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.RefreshToken));
    }

    private static string UniqueEmail()
    {
        return $"test-{Guid.NewGuid():N}@example.com";
    }
}
