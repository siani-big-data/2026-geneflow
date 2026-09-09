namespace GeneFlow.ApiNet2.Application.Identity.Interfaces;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification message.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="username">The recipient's username.</param>
    /// <param name="verificationToken">The verification token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailVerificationAsync(
        string email,
        string username,
        string verificationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="username">The recipient's username.</param>
    /// <param name="resetToken">The password reset token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetAsync(
        string email,
        string username,
        string resetToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a two-factor authentication code.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="username">The recipient's username.</param>
    /// <param name="code">The 2FA code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendTwoFactorCodeAsync(
        string email,
        string username,
        string code,
        CancellationToken cancellationToken = default);
}
