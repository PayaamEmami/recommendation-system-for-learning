using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rsl.Api.DTOs.Auth.Requests;
using Rsl.Api.DTOs.Auth.Responses;
using Rsl.Api.DTOs.Sources.Requests;
using Rsl.Api.DTOs.Sources.Responses;
using Rsl.Core.Enums;
using Rsl.Tests.Infrastructure;

namespace Rsl.Tests.Integration;

[TestClass]
public sealed class SourcesIntegrationTests
{
    private static ApiWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
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
    public async Task CreateUpdateDeleteSource_WorksForAuthorizedUser()
    {
        var accessToken = await RegisterAndGetAccessTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createRequest = new CreateSourceRequest
        {
            Name = "Test Source",
            Url = "https://example.com/rss",
            Description = "Test source description",
            Category = ResourceType.BlogPost,
            IsActive = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/Sources", createRequest);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<SourceResponse>(JsonOptions);
        Assert.IsNotNull(created);
        Assert.AreEqual(createRequest.Name, created.Name);

        var listResponse = await _client.GetFromJsonAsync<List<SourceResponse>>("/api/v1/Sources", JsonOptions);
        Assert.IsNotNull(listResponse);
        Assert.HasCount(1, listResponse);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/Sources/{created.Id}", new UpdateSourceRequest
        {
            Name = "Updated Source",
            IsActive = false
        });

        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<SourceResponse>(JsonOptions);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Source", updated.Name);
        Assert.IsFalse(updated.IsActive);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/Sources/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getDeletedResponse = await _client.GetAsync($"/api/v1/Sources/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client)
    {
        var request = new RegisterRequest
        {
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            DisplayName = "Test User"
        };

        var response = await client.PostAsJsonAsync("/api/v1/Auth/register", request);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(payload);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.AccessToken));

        return payload.AccessToken;
    }
}
