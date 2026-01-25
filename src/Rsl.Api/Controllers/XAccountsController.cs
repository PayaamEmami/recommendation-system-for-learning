using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Rsl.Api.DTOs.X.Requests;
using Rsl.Api.DTOs.X.Responses;
using Rsl.Api.Extensions;
using Rsl.Api.Services;

namespace Rsl.Api.Controllers;

/// <summary>
/// Handles X account connection and feed operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/x")]
[Authorize]
[EnableRateLimiting("api")]
public class XAccountsController : ControllerBase
{
    private readonly IXAccountService _xAccountService;
    private readonly ILogger<XAccountsController> _logger;
    private readonly IConfiguration _configuration;

    public XAccountsController(
        IXAccountService xAccountService,
        ILogger<XAccountsController> logger,
        IConfiguration configuration)
    {
        _xAccountService = xAccountService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Creates an authorization URL to connect an X account.
    /// </summary>
    [HttpGet("connect-url")]
    [ProducesResponseType(typeof(XConnectUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConnectUrl([FromQuery] string? redirectUri, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        string? resolvedRedirectUri = null;
        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirectUriParsed))
            {
                return BadRequest("Invalid redirectUri");
            }

            var origin = redirectUriParsed.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            var isAllowed = allowedOrigins.Any(allowed =>
                string.Equals(allowed.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                return BadRequest("Redirect URI is not allowed");
            }

            resolvedRedirectUri = redirectUri;
        }

        try
        {
            var url = await _xAccountService.CreateConnectUrlAsync(userId.Value, resolvedRedirectUri, cancellationToken);
            return Ok(new XConnectUrlResponse { AuthorizationUrl = url });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Handles the OAuth callback and stores tokens.
    /// </summary>
    [HttpPost("callback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleCallback([FromBody] XCallbackRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.State))
        {
            return BadRequest("Missing code or state");
        }

        await _xAccountService.HandleCallbackAsync(userId.Value, request.Code, request.State, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets followed X accounts, optionally refreshing from X.
    /// </summary>
    [HttpGet("followed-accounts")]
    [ProducesResponseType(typeof(List<XFollowedAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowedAccounts([FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        List<Rsl.Core.Entities.XFollowedAccount> followed;
        try
        {
            followed = refresh
                ? await _xAccountService.RefreshFollowedAccountsAsync(userId.Value, cancellationToken)
                : await _xAccountService.GetFollowedAccountsAsync(userId.Value, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        var selected = await _xAccountService.GetSelectedAccountsAsync(userId.Value, cancellationToken);
        var selectedIds = selected.Select(s => s.XFollowedAccountId).ToHashSet();

        var response = followed.Select(account => new XFollowedAccountResponse
        {
            Id = account.Id,
            XUserId = account.XUserId,
            Handle = account.Handle,
            DisplayName = account.DisplayName,
            ProfileImageUrl = account.ProfileImageUrl,
            IsSelected = selectedIds.Contains(account.Id)
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Updates the selected followed accounts used for the X feed.
    /// </summary>
    [HttpPost("selected-accounts")]
    [ProducesResponseType(typeof(List<XFollowedAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSelectedAccounts([FromBody] XSelectedAccountsRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await _xAccountService.UpdateSelectedAccountsAsync(userId.Value, request.FollowedAccountIds, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        var followed = await _xAccountService.GetFollowedAccountsAsync(userId.Value, cancellationToken);
        var selected = await _xAccountService.GetSelectedAccountsAsync(userId.Value, cancellationToken);
        var selectedIds = selected.Select(s => s.XFollowedAccountId).ToHashSet();

        var response = followed.Select(account => new XFollowedAccountResponse
        {
            Id = account.Id,
            XUserId = account.XUserId,
            Handle = account.Handle,
            DisplayName = account.DisplayName,
            ProfileImageUrl = account.ProfileImageUrl,
            IsSelected = selectedIds.Contains(account.Id)
        }).ToList();

        _logger.LogInformation("Updated X selected accounts for user {UserId}", userId.Value);
        return Ok(response);
    }

    /// <summary>
    /// Gets the stored X posts feed for the user.
    /// </summary>
    [HttpGet("posts")]
    [ProducesResponseType(typeof(List<XPostResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPosts([FromQuery] int limit = 30, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        List<Rsl.Core.Entities.XPost> posts;
        try
        {
            posts = await _xAccountService.GetPostsAsync(userId.Value, limit, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        var response = posts.Select(post => new XPostResponse
        {
            Id = post.Id,
            PostId = post.PostId,
            Text = post.Text,
            Url = post.Url,
            PostCreatedAt = post.PostCreatedAt,
            AuthorHandle = post.AuthorHandle,
            AuthorName = post.AuthorName,
            AuthorProfileImageUrl = post.AuthorProfileImageUrl,
            MediaJson = post.MediaJson,
            LikeCount = post.LikeCount,
            ReplyCount = post.ReplyCount,
            RepostCount = post.RepostCount,
            QuoteCount = post.QuoteCount
        }).ToList();

        return Ok(response);
    }
}
