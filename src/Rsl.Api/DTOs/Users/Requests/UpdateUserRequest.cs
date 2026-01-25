using System.ComponentModel.DataAnnotations;

namespace Rsl.Api.DTOs.Users.Requests;

/// <summary>
/// Request model for updating user profile information.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// The user's display name.
    /// </summary>
    [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
    public string? DisplayName { get; set; }
}

