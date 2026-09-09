using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// Marker interface for commands/queries that require the user to be a member of a study.
/// When a request implements this interface, the StudyMembershipBehavior will
/// validate that the user is a member of the specified study before the handler is invoked.
/// </summary>
public interface IRequireStudyMembership : IRequireAuthentication
{
    /// <summary>
    /// The ID of the study that requires membership validation.
    /// </summary>
    string StudyId { get; }

    /// <summary>
    /// The minimum role required for this operation.
    /// Defaults to null (any member can access, or public study for read-only).
    /// Override to require Editor, Admin, or Owner.
    /// </summary>
    StudyRole? MinimumRole => null;
}
