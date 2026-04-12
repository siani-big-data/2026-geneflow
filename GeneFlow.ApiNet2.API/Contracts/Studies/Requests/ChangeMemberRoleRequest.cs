using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for changing a member's role.
/// </summary>
public sealed record ChangeMemberRoleRequest
{
    /// <summary>The new role ID. Valid values: 2=Admin, 3=Editor, 4=Viewer.</summary>
    [Required]
    [Range(2, 4)]
    public required int NewRoleId { get; init; }
}
