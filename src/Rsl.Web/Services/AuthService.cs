namespace Rsl.Web.Services;

/// <summary>
/// Simple authentication service for managing user sessions.
/// In production, this would integrate with ASP.NET Core Identity or a proper auth system.
/// </summary>
public class AuthService
{
    private AuthState _currentState = new();
    private readonly List<UserAccount> _users = new();

    public event Action? OnAuthStateChanged;

    public AuthState CurrentState => _currentState;

    public AuthService()
    {
        // Add a default test user for development
        _users.Add(new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = HashPassword("password123")
        });
    }

    public async Task<AuthResult> SignUpAsync(string email, string password, string? displayName)
    {
        await Task.Delay(100); // Simulate async operation

        if (string.IsNullOrWhiteSpace(email))
            return new AuthResult { Success = false, ErrorMessage = "Email is required" };

        if (string.IsNullOrWhiteSpace(password))
            return new AuthResult { Success = false, ErrorMessage = "Password is required" };

        if (_users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return new AuthResult { Success = false, ErrorMessage = "Email already exists" };

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email.ToLower(),
            DisplayName = displayName ?? email,
            PasswordHash = HashPassword(password)
        };

        _users.Add(user);

        _currentState = new AuthState
        {
            IsAuthenticated = true,
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        };

        OnAuthStateChanged?.Invoke();
        return new AuthResult { Success = true };
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        await Task.Delay(100); // Simulate async operation

        var user = _users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (user == null || !VerifyPassword(password, user.PasswordHash))
            return new AuthResult { Success = false, ErrorMessage = "Invalid email or password" };

        _currentState = new AuthState
        {
            IsAuthenticated = true,
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        };

        OnAuthStateChanged?.Invoke();
        return new AuthResult { Success = true };
    }

    public void Logout()
    {
        _currentState = new AuthState();
        OnAuthStateChanged?.Invoke();
    }

    private static string HashPassword(string password)
    {
        // Simple hash for demo purposes - use proper password hashing in production
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}

public class AuthState
{
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UserAccount
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
