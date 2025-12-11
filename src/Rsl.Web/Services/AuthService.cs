using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Rsl.Web.Services;

/// <summary>
/// Authentication service that integrates with the RSL API.
/// </summary>
public class AuthService
{
    private AuthState _currentState = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public event Action? OnAuthStateChanged;

    public AuthState CurrentState => _currentState;

    public bool IsRegistrationEnabled =>
        _configuration.GetValue<bool>("Registration:Enabled", true);

    public string RegistrationDisabledMessage =>
        _configuration.GetValue<string>("Registration:DisabledMessage",
            "New account registrations are currently closed. Please check back later.") ?? string.Empty;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl");
        if (!string.IsNullOrEmpty(apiBaseUrl))
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }
        return client;
    }

    public async Task<AuthResult> SignUpAsync(string email, string password, string? displayName)
    {
        try
        {
            // Check if registrations are enabled
            if (!IsRegistrationEnabled)
                return new AuthResult { Success = false, ErrorMessage = RegistrationDisabledMessage };

            var request = new
            {
                email = email,
                password = password,
                displayName = displayName
            };

            using var httpClient = CreateHttpClient();
            var response = await httpClient.PostAsJsonAsync("/api/v1/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    _currentState = new AuthState
                    {
                        IsAuthenticated = true,
                        UserId = result.User.Id,
                        Email = result.User.Email,
                        DisplayName = result.User.DisplayName,
                        AccessToken = result.AccessToken
                    };

                    OnAuthStateChanged?.Invoke();
                    return new AuthResult { Success = true };
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Registration failed: {StatusCode} - {Content}", response.StatusCode, errorContent);

            return new AuthResult { Success = false, ErrorMessage = "Registration failed. Please try again." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new AuthResult { Success = false, ErrorMessage = "An error occurred. Please try again." };
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var request = new
            {
                email = email,
                password = password
            };

            using var httpClient = CreateHttpClient();
            var response = await httpClient.PostAsJsonAsync("/api/v1/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    _currentState = new AuthState
                    {
                        IsAuthenticated = true,
                        UserId = result.User.Id,
                        Email = result.User.Email,
                        DisplayName = result.User.DisplayName,
                        AccessToken = result.AccessToken
                    };

                    OnAuthStateChanged?.Invoke();
                    return new AuthResult { Success = true };
                }
            }

            return new AuthResult { Success = false, ErrorMessage = "Invalid email or password" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new AuthResult { Success = false, ErrorMessage = "An error occurred. Please try again." };
        }
    }

    public void Logout()
    {
        _currentState = new AuthState();
        OnAuthStateChanged?.Invoke();
    }
}

public class AuthState
{
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AccessToken { get; set; }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = null!;
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
