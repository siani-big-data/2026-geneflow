namespace GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;

/// <summary>
/// Account lockout configuration settings.
/// </summary>
public sealed class LockoutSettings
{
    public const string SectionName = "Lockout";

    /// <summary>
    /// Maximum failed login attempts before lockout.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;
}
