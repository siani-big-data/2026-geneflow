using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Studies.Entities;

/// <summary>
/// Represents an invitation to join a study.
/// </summary>
public sealed class StudyInvitation : AuditableEntity<StudyInvitationId>
{
    public const int TokenLength = 64;
    public const int MaxMessageLength = 500;
    public static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(7);

    public StudyId StudyId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public StudyRole Role { get; private set; } = null!;
    public InvitationStatus Status { get; private set; } = null!;
    public string Token { get; private set; } = null!;
    public UserId InvitedBy { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public string? Message { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsPending => Status == InvitationStatus.Pending && !IsExpired;

    private StudyInvitation() : base(new StudyInvitationId(0)) { }

    private StudyInvitation(
        StudyInvitationId id,
        StudyId studyId,
        string email,
        StudyRole role,
        UserId invitedBy,
        string? message) : base(id)
    {
        StudyId = studyId;
        Email = email.ToLowerInvariant();
        Role = role;
        Status = InvitationStatus.Pending;
        Token = GenerateToken();
        InvitedBy = invitedBy;
        ExpiresAt = DateTime.UtcNow.Add(DefaultExpiration);
        Message = message?.Trim();

        SetCreationAudit(DateTime.UtcNow, invitedBy.Value.ToString());
    }

    public static Result<StudyInvitation> Create(
        StudyInvitationId id,
        StudyId studyId,
        string email,
        StudyRole role,
        UserId invitedBy,
        string? message = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<StudyInvitation>(Error.Validation("Invitation.EmailRequired", "Email is required."));

        if (role == StudyRole.Owner)
            return Result.Failure<StudyInvitation>(Error.Validation("Invitation.CannotInviteAsOwner", "Cannot invite as owner."));

        if (message?.Length > MaxMessageLength)
            return Result.Failure<StudyInvitation>(Error.Validation("Invitation.MessageTooLong", $"Message cannot exceed {MaxMessageLength} characters."));

        return new StudyInvitation(id, studyId, email, role, invitedBy, message);
    }

    public Result Accept()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(StudyErrors.InvitationAlreadyResponded);

        if (IsExpired)
        {
            Status = InvitationStatus.Expired;
            return Result.Failure(StudyErrors.InvitationExpired);
        }

        Status = InvitationStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Decline()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(StudyErrors.InvitationAlreadyResponded);

        if (IsExpired)
        {
            Status = InvitationStatus.Expired;
            return Result.Failure(StudyErrors.InvitationExpired);
        }

        Status = InvitationStatus.Declined;
        RespondedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(StudyErrors.InvitationAlreadyResponded);

        Status = InvitationStatus.Cancelled;
        return Result.Success();
    }

    public Result Resend()
    {
        if (!Status.CanBeResent)
            return Result.Failure(Error.Validation("Invitation.CannotResend", "This invitation cannot be resent."));

        Status = InvitationStatus.Pending;
        Token = GenerateToken();
        ExpiresAt = DateTime.UtcNow.Add(DefaultExpiration);
        RespondedAt = null;
        return Result.Success();
    }

    private static string GenerateToken()
    {
        var bytes = new byte[TokenLength / 2];
        Random.Shared.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
