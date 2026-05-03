using System.Net.Sockets;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Services;

/// <summary>
/// Email service implementation using MailKit.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the EmailService.
    /// </summary>
    public EmailService(
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendEmailVerificationAsync(
        string email,
        string username,
        string token,
        CancellationToken cancellationToken = default)
    {
        var verificationUrl = $"{_settings.BaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";

        var subject = "Verify your email address";
        var body = $"""
            Hello {username},

            Welcome to GeneFlow! Please verify your email address by clicking the link below:

            {verificationUrl}

            This link will expire in 24 hours.

            If you did not create an account, please ignore this email.

            Best regards,
            The GeneFlow Team
            """;

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendTwoFactorCodeAsync(
        string email,
        string username,
        string code,
        CancellationToken cancellationToken = default)
    {
        var subject = "Your verification code";
        var body = $"""
            Hello {username},

            Your two-factor authentication code is:

            {code}

            This code will expire in 10 minutes.

            If you did not request this code, please secure your account immediately.

            Best regards,
            The GeneFlow Team
            """;

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetAsync(
        string email,
        string username,
        string token,
        CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_settings.BaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var subject = "Reset your password";
        var body = $"""
            Hello {username},

            We received a request to reset your password. Click the link below to set a new password:

            {resetUrl}

            This link will expire in 1 hour.

            If you did not request a password reset, please ignore this email.

            Best regards,
            The GeneFlow Team
            """;

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    private async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();

            var secureSocketOptions = _settings.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.SmtpUsername) &&
                !string.IsNullOrWhiteSpace(_settings.SmtpPassword))
            {
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(ex, "SMTP command error sending email to {Email}", toEmail);
            throw;
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogError(ex, "SMTP protocol error sending email to {Email}", toEmail);
            throw;
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex, "SMTP authentication failed sending email to {Email}", toEmail);
            throw;
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Network error sending email to {Email}", toEmail);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error sending email to {Email}", toEmail);
            throw;
        }
    }
}
