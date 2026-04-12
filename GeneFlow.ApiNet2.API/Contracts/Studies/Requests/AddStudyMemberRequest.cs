using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for adding a member to a study.
/// </summary>
public sealed record AddStudyMemberRequest
{
    /// <summary>The user ID to add as a member.</summary>
    [Required]
    public required string UserId { get; init; }

    /// <summary>The role ID for the member. Valid values: 2=Admin, 3=Editor, 4=Viewer.</summary>
    [Required]
    [Range(2, 4)]
    public required int RoleId { get; init; }
}
