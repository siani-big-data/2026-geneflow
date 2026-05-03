namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// Marker interface for commands that require validation of the study member limit.
/// When a request implements this interface, the SubscriptionLimitBehavior will
/// validate that the study owner hasn't exceeded their plan's MaxMembersPerStudy limit.
/// </summary>
public interface IRequiresMemberLimit : IRequireAuthentication
{
    /// <summary>
    /// The ID of the study where members are being added.
    /// Used to look up the study owner's subscription limits.
    /// </summary>
    string StudyId { get; }
}
