namespace Rsl.Api.Configuration;

/// <summary>
/// Configuration settings for user registration.
/// </summary>
public class RegistrationSettings
{
    /// <summary>
    /// Gets or sets whether new user registrations are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the message to display when registrations are disabled.
    /// </summary>
    public string DisabledMessage { get; set; } = "New account registrations are currently closed. Please check back later.";
}
