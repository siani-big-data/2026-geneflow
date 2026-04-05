using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Identity.EventHandlers;

/// <summary>
/// Sends two-factor authentication code via email when generated.
/// </summary>
public sealed class SendTwoFactorCodeEmailHandler
    : IDomainEventHandler<TwoFactorCodeGeneratedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendTwoFactorCodeEmailHandler> _logger;

    public SendTwoFactorCodeEmailHandler(
        IEmailService emailService,
        ILogger<SendTwoFactorCodeEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(TwoFactorCodeGeneratedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending 2FA code to {Email} for user {UserId}",
            notification.Email,
            notification.UserId);

        try
        {
            await _emailService.SendTwoFactorCodeAsync(
                notification.Email,
                notification.Username,
                notification.Code,
                cancellationToken);

            _logger.LogInformation(
                "2FA code sent successfully to {Email}",
                notification.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send 2FA code to {Email}. User will not receive authentication code.",
                notification.Email);
            // Don't rethrow - but this is critical, user won't be able to login without the code
        }
    }
}
