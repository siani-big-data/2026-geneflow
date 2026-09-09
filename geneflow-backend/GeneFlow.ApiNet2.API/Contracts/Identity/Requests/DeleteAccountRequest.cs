using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for deleting user account.
/// </summary>
public sealed record DeleteAccountRequest
{
    /// <summary>Confirmation text (must be "DELETE").</summary>
    [Required]
    public required string ConfirmationText { get; init; }
}
