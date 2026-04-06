namespace GeneFlow.ApiNet2.Application.Profiles.DTOs;

/// <summary>
/// Data transfer object for profile statistics.
/// </summary>
public sealed record ProfileStatsDto
{
    /// <summary>Gets the total number of studies the user is a member of.</summary>
    public required int TotalStudies { get; init; }

    /// <summary>Gets the number of studies owned by the user.</summary>
    public required int OwnedStudies { get; init; }

    /// <summary>Gets the total number of traces uploaded by the user.</summary>
    public required int TotalTraces { get; init; }

    /// <summary>Gets the total number of alignments created by the user.</summary>
    public required int TotalAlignments { get; init; }

    /// <summary>Gets the number of completed alignments.</summary>
    public required int CompletedAlignments { get; init; }

    /// <summary>Gets the last activity timestamp.</summary>
    public DateTime? LastActivityAt { get; init; }

    /// <summary>Gets when the user became a member.</summary>
    public required DateTime MemberSince { get; init; }
}
