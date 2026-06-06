using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Notifications;

public static class WatchErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Watch.NotFound", "Watch subscription was not found.");
}
