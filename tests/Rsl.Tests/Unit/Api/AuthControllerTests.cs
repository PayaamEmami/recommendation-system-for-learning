using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rsl.Api.Configuration;
using Rsl.Api.Controllers;
using Rsl.Api.DTOs.Auth.Requests;
using Rsl.Api.DTOs.Auth.Responses;
using Rsl.Api.Services;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class AuthControllerTests
{
    [TestMethod]
    public void GetRegistrationStatus_WhenEnabled_ReturnsEnabledTrue()
    {
        var authService = new Mock<IAuthService>(MockBehavior.Strict);
        var controller = new AuthController(
            authService.Object,
            NullLogger<AuthController>.Instance,
            new RegistrationSettings { Enabled = true });

        var result = controller.GetRegistrationStatus();

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var value = okResult.Value!;
        Assert.IsTrue(GetProperty<bool>(value, "enabled"));
        Assert.IsNull(GetProperty<string?>(value, "message"));
    }

    [TestMethod]
    public void GetRegistrationStatus_WhenDisabled_ReturnsMessage()
    {
        var authService = new Mock<IAuthService>(MockBehavior.Strict);
        var controller = new AuthController(
            authService.Object,
            NullLogger<AuthController>.Instance,
            new RegistrationSettings { Enabled = false, DisabledMessage = "Registrations disabled" });

        var result = controller.GetRegistrationStatus();

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var value = okResult.Value!;
        Assert.IsFalse(GetProperty<bool>(value, "enabled"));
        Assert.AreEqual("Registrations disabled", GetProperty<string?>(value, "message"));
    }

    [TestMethod]
    public async Task Register_WhenDisabled_ReturnsForbiddenProblem()
    {
        var authService = new Mock<IAuthService>(MockBehavior.Strict);
        var controller = new AuthController(
            authService.Object,
            NullLogger<AuthController>.Instance,
            new RegistrationSettings { Enabled = false, DisabledMessage = "Registrations disabled" });

        var result = await controller.Register(new RegisterRequest(), CancellationToken.None);

        var objectResult = result as ObjectResult;
        Assert.IsNotNull(objectResult);
        Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);

        var problem = objectResult.Value as ProblemDetails;
        Assert.IsNotNull(problem);
        Assert.AreEqual("Registration Disabled", problem.Title);
        Assert.AreEqual("Registrations disabled", problem.Detail);
    }

    [TestMethod]
    public async Task Login_ReturnsOkWithResponse()
    {
        var response = new LoginResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh"
        };
        var authService = new Mock<IAuthService>(MockBehavior.Strict);
        authService
            .Setup(service => service.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new AuthController(
            authService.Object,
            NullLogger<AuthController>.Instance,
            new RegistrationSettings { Enabled = true });

        var result = await controller.Login(new LoginRequest(), CancellationToken.None);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreSame(response, okResult.Value);
    }

    [TestMethod]
    public async Task RefreshToken_ReturnsOkWithResponse()
    {
        var response = new RefreshTokenResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh"
        };
        var authService = new Mock<IAuthService>(MockBehavior.Strict);
        authService
            .Setup(service => service.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new AuthController(
            authService.Object,
            NullLogger<AuthController>.Instance,
            new RegistrationSettings { Enabled = true });

        var result = await controller.RefreshToken(new RefreshTokenRequest(), CancellationToken.None);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreSame(response, okResult.Value);
    }

    private static T? GetProperty<T>(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name);
        return property == null ? default : (T?)property.GetValue(instance);
    }
}
