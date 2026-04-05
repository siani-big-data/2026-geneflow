using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Identity.EventHandlers;

/// <summary>
/// Sends verification email when a user registers.
/// </summary>
public sealed class SendVerificationEmailOnUserRegisteredHandler
    : IDomainEventHandler<UserRegisteredEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendVerificationEmailOnUserRegisteredHandler> _logger;

    public SendVerificationEmailOnUserRegisteredHandler(
        IEmailService emailService,
        ILogger<SendVerificationEmailOnUserRegisteredHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending verification email to {Email} for user {UserId}",
            notification.Email,
            notification.UserId);

        try
        {
            await _emailService.SendEmailVerificationAsync(
                notification.Email,
                notification.Username,
                notification.EmailVerificationToken,
                cancellationToken);

            _logger.LogInformation(
                "Verification email sent successfully to {Email}",
                notification.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send verification email to {Email}",
                notification.Email);
            // Don't rethrow - email failure shouldn't fail registration
        }
    }
}
