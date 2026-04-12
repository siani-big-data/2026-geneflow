using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for sending a study invitation.
/// </summary>
public sealed record SendInvitationRequest
{
    /// <summary>The email address to invite.</summary>
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public required string Email { get; init; }

    /// <summary>The role ID for the invitee. Valid values: 2=Admin, 3=Editor, 4=Viewer.</summary>
    [Required]
    [Range(2, 4)]
    public required int RoleId { get; init; }

    /// <summary>An optional message to include with the invitation (max 500 characters).</summary>
    [StringLength(500)]
    public string? Message { get; init; }
}
