using System.Security.Claims;
using Rsl.Api.Extensions;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class ClaimsPrincipalExtensionsTests
{
    [TestMethod]
    public void GetUserId_WhenClaimMissing_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = principal.GetUserId();

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetUserId_WhenClaimInvalid_ReturnsNull()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") });
        var principal = new ClaimsPrincipal(identity);

        var result = principal.GetUserId();

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetUserId_WhenValid_ReturnsGuid()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) });
        var principal = new ClaimsPrincipal(identity);

        var result = principal.GetUserId();

        Assert.AreEqual(userId, result);
    }

    [TestMethod]
    public void GetUserEmail_WhenClaimMissing_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = principal.GetUserEmail();

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetUserEmail_WhenClaimPresent_ReturnsEmail()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "user@example.com") });
        var principal = new ClaimsPrincipal(identity);

        var result = principal.GetUserEmail();

        Assert.AreEqual("user@example.com", result);
    }
}
