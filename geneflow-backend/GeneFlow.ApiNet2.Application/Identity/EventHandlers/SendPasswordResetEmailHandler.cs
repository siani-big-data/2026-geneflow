using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Identity.EventHandlers;

/// <summary>
/// Sends password reset email when a user requests a password reset.
/// </summary>
/// <remarks>
/// Exceptions are intentionally caught and logged without rethrowing because
/// email-delivery failures must not abort the password-reset request flow.
/// </remarks>
public sealed class SendPasswordResetEmailHandler
    : IDomainEventHandler<PasswordResetRequestedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendPasswordResetEmailHandler> _logger;

    public SendPasswordResetEmailHandler(
        IEmailService emailService,
        ILogger<SendPasswordResetEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PasswordResetRequestedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending password reset email to {Email} for user {UserId}",
            notification.Email,
            notification.UserId);

        try
        {
            await _emailService.SendPasswordResetAsync(
                notification.Email,
                notification.Username,
                notification.ResetToken,
                cancellationToken);

            _logger.LogInformation(
                "Password reset email sent successfully to {Email}",
                notification.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send password reset email to {Email}",
                notification.Email);
        }
    }
}
