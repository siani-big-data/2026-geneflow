using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// Marker interface for commands/queries that require access to a trace and its parent study.
/// When a request implements this interface, the TraceAccessBehavior will
/// look up the trace's study and validate that the user is a member with the required role.
/// </summary>
public interface IRequireTraceAccess : IRequireAuthentication
{
    /// <summary>
    /// The ID of the trace that requires access validation.
    /// </summary>
    string TraceId { get; }

    /// <summary>
    /// The minimum role required for this operation.
    /// Defaults to null (any member can access, or public study for read-only).
    /// Override to require Editor, Admin, or Owner.
    /// </summary>
    StudyRole? MinimumRole => null;
}
