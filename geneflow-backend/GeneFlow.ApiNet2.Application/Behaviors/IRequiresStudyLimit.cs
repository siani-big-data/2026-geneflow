namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// Marker interface for commands that require validation of the study creation limit.
/// When a request implements this interface, the SubscriptionLimitBehavior will
/// validate that the user hasn't exceeded their plan's MaxStudies limit.
/// </summary>
public interface IRequiresStudyLimit : IRequireAuthentication
{
}
