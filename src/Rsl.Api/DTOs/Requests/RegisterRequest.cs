using System.ComponentModel.DataAnnotations;

namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// The user's email address (will be used for login).
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The user's display name.
    /// </summary>
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
    public string DisplayName { get; set; } = string.Empty;
}

