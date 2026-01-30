using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Rsl.Tests.Unit.Api;

internal static class ControllerTestHelpers
{
    public static void SetUser(ControllerBase controller, Guid? userId, string? email = null)
    {
        var identity = new ClaimsIdentity();

        if (userId.HasValue)
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
