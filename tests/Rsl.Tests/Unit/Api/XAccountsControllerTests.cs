using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Api.Controllers;
using Rsl.Api.DTOs.X.Requests;
using Rsl.Api.DTOs.X.Responses;
using Rsl.Api.Services;
using Rsl.Core.Entities;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class XAccountsControllerTests
{
    private static XAccountsController CreateController(
        IConfiguration configuration,
        out Mock<IXAccountService> service)
    {
        service = new Mock<IXAccountService>(MockBehavior.Strict);
        return new XAccountsController(service.Object, NullLogger<XAccountsController>.Instance, configuration);
    }

    private static IConfiguration CreateConfiguration(params string[] allowedOrigins)
    {
        var values = new Dictionary<string, string?>();
        for (var index = 0; index < allowedOrigins.Length; index++)
        {
            values[$"Cors:AllowedOrigins:{index}"] = allowedOrigins[index];
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [TestMethod]
    public async Task GetConnectUrl_WhenMissingUser_ReturnsUnauthorized()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out _);
        ControllerTestHelpers.SetUser(controller, null);

        var result = await controller.GetConnectUrl(null, CancellationToken.None);

        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    [TestMethod]
    public async Task GetConnectUrl_WhenRedirectInvalid_ReturnsBadRequest()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out _);
        ControllerTestHelpers.SetUser(controller, Guid.NewGuid());

        var result = await controller.GetConnectUrl("not-a-url", CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("Invalid redirectUri", badRequest.Value);
    }

    [TestMethod]
    public async Task GetConnectUrl_WhenRedirectNotAllowed_ReturnsBadRequest()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out _);
        ControllerTestHelpers.SetUser(controller, Guid.NewGuid());

        var result = await controller.GetConnectUrl("https://blocked.com/callback", CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("Redirect URI is not allowed", badRequest.Value);
    }

    [TestMethod]
    public async Task GetConnectUrl_WhenServiceThrows_ReturnsBadRequest()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        service.Setup(svc => svc.CreateConnectUrlAsync(userId, "https://allowed.com/callback", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("missing"));

        var result = await controller.GetConnectUrl("https://allowed.com/callback", CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("missing", badRequest.Value);
    }

    [TestMethod]
    public async Task GetConnectUrl_WhenSuccess_ReturnsOk()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        service.Setup(svc => svc.CreateConnectUrlAsync(userId, "https://allowed.com/callback", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://x.com/oauth");

        var result = await controller.GetConnectUrl("https://allowed.com/callback", CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var response = ok.Value as XConnectUrlResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual("https://x.com/oauth", response.AuthorizationUrl);
    }

    [TestMethod]
    public async Task HandleCallback_WhenMissingUser_ReturnsUnauthorized()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out _);
        ControllerTestHelpers.SetUser(controller, null);

        var result = await controller.HandleCallback(new XCallbackRequest(), CancellationToken.None);

        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    [TestMethod]
    public async Task HandleCallback_WhenMissingCodeOrState_ReturnsBadRequest()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out _);
        ControllerTestHelpers.SetUser(controller, Guid.NewGuid());

        var result = await controller.HandleCallback(new XCallbackRequest(), CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("Missing code or state", badRequest.Value);
    }

    [TestMethod]
    public async Task HandleCallback_WhenSuccess_ReturnsNoContent()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        service.Setup(svc => svc.HandleCallbackAsync(userId, "code", "state", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await controller.HandleCallback(new XCallbackRequest { Code = "code", State = "state" }, CancellationToken.None);

        Assert.IsInstanceOfType<NoContentResult>(result);
    }

    [TestMethod]
    public async Task GetFollowedAccounts_WhenMissingUser_ReturnsUnauthorized()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out _);
        ControllerTestHelpers.SetUser(controller, null);

        var result = await controller.GetFollowedAccounts(false, CancellationToken.None);

        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    [TestMethod]
    public async Task GetFollowedAccounts_WhenServiceThrows_ReturnsBadRequest()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        service.Setup(svc => svc.GetFollowedAccountsAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not connected"));

        var result = await controller.GetFollowedAccounts(false, CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("not connected", badRequest.Value);
    }

    [TestMethod]
    public async Task GetFollowedAccounts_WhenSuccess_ReturnsOk()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        var followedId = Guid.NewGuid();
        service.Setup(svc => svc.GetFollowedAccountsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<XFollowedAccount>
            {
                new() { Id = followedId, XUserId = "x", Handle = "handle", DisplayName = "Name" }
            });
        service.Setup(svc => svc.GetSelectedAccountsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<XSelectedAccount>
            {
                new() { XFollowedAccountId = followedId }
            });

        var result = await controller.GetFollowedAccounts(false, CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var response = ok.Value as List<XFollowedAccountResponse>;
        Assert.IsNotNull(response);
        Assert.HasCount(1, response);
        Assert.IsTrue(response[0].IsSelected);
    }

    [TestMethod]
    public async Task UpdateSelectedAccounts_WhenMissingUser_ReturnsUnauthorized()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out _);
        ControllerTestHelpers.SetUser(controller, null);

        var result = await controller.UpdateSelectedAccounts(new XSelectedAccountsRequest(), CancellationToken.None);

        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    [TestMethod]
    public async Task UpdateSelectedAccounts_WhenServiceThrows_ReturnsBadRequest()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        service.Setup(svc => svc.UpdateSelectedAccountsAsync(userId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var result = await controller.UpdateSelectedAccounts(new XSelectedAccountsRequest(), CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("invalid", badRequest.Value);
    }

    [TestMethod]
    public async Task UpdateSelectedAccounts_WhenSuccess_ReturnsOk()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        var followedId = Guid.NewGuid();
        service.Setup(svc => svc.UpdateSelectedAccountsAsync(userId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<XSelectedAccount>());
        service.Setup(svc => svc.GetFollowedAccountsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<XFollowedAccount> { new() { Id = followedId } });
        service.Setup(svc => svc.GetSelectedAccountsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<XSelectedAccount> { new() { XFollowedAccountId = followedId } });

        var result = await controller.UpdateSelectedAccounts(new XSelectedAccountsRequest { FollowedAccountIds = new List<Guid> { followedId } }, CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var response = ok.Value as List<XFollowedAccountResponse>;
        Assert.IsNotNull(response);
        Assert.HasCount(1, response);
        Assert.IsTrue(response[0].IsSelected);
    }

    [TestMethod]
    public async Task GetPosts_WhenMissingUser_ReturnsUnauthorized()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out _);
        ControllerTestHelpers.SetUser(controller, null);

        var result = await controller.GetPosts(10, CancellationToken.None);

        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    [TestMethod]
    public async Task GetPosts_WhenServiceThrows_ReturnsBadRequest()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        service.Setup(svc => svc.GetPostsAsync(userId, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var result = await controller.GetPosts(10, CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("invalid", badRequest.Value);
    }

    [TestMethod]
    public async Task GetPosts_WhenSuccess_ReturnsOk()
    {
        var controller = CreateController(CreateConfiguration("https://allowed.com"), out var service);
        var userId = Guid.NewGuid();
        ControllerTestHelpers.SetUser(controller, userId);

        var posts = new List<XPost>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PostId = "post",
                Text = "text",
                Url = "https://x.com/post",
                PostCreatedAt = DateTime.UtcNow,
                AuthorHandle = "handle",
                AuthorName = "name",
                LikeCount = 1
            }
        };

        service.Setup(svc => svc.GetPostsAsync(userId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var result = await controller.GetPosts(10, CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var response = ok.Value as List<XPostResponse>;
        Assert.IsNotNull(response);
        Assert.HasCount(1, response);
        Assert.AreEqual("post", response[0].PostId);
    }
}
