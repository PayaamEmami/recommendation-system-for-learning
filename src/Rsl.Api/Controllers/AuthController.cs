using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rsl.Api.DTOs.Requests;
using Rsl.Api.Services;

namespace Rsl.Api.Controllers;

/// <summary>
/// Handles authentication operations (login, registration, token refresh).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens.
    /// </summary>
    /// <param name="request">The login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token, refresh token, and user information.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(DTOs.Responses.LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token, refresh token, and user information.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(DTOs.Responses.LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Refreshes an expired access token using a refresh token.
    /// </summary>
    /// <param name="request">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New access token and refresh token.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(DTOs.Responses.RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(response);
    }
}

