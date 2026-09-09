namespace GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;

/// <summary>
/// Configuration settings for email service.
/// </summary>
public sealed class EmailSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Email";

    /// <summary>Gets or sets the SMTP server host.</summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>Gets or sets the SMTP server port.</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>Gets or sets the SMTP username for authentication.</summary>
    public string? SmtpUsername { get; set; }

    /// <summary>Gets or sets the SMTP password for authentication.</summary>
    public string? SmtpPassword { get; set; }

    /// <summary>Gets or sets whether to use SSL/TLS.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Gets or sets the email address to send from.</summary>
    public string FromEmail { get; set; } = "noreply@geneflow.com";

    /// <summary>Gets or sets the display name for the sender.</summary>
    public string FromName { get; set; } = "GeneFlow";

    /// <summary>Gets or sets the base URL for the application (used in email links).</summary>
    public string BaseUrl { get; set; } = "http://localhost:5145";
}
