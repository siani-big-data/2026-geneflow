using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Notifications;

public static class NotificationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Notification.NotFound", "Notification was not found.");

    public static Error NotFoundById(string id) =>
        Error.NotFound("Notification.NotFoundById", $"Notification with ID '{id}' was not found.");

    public static readonly Error NotRecipient =
        Error.Forbidden("Notification.NotRecipient", "You can only modify your own notifications.");

    public static readonly Error SubjectRequired =
        Error.Validation("Notification.SubjectRequired", "Notification subject is required.");

    public static readonly Error SubjectTooLong =
        Error.Validation("Notification.SubjectTooLong", "Notification subject must be 300 characters or less.");
}
