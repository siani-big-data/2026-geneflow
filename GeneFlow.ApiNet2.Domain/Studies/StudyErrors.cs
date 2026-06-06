using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Studies;

/// <summary>
/// Contains all domain errors related to the Study aggregate.
/// </summary>
public static class StudyErrors
{
    // General errors
    public static readonly Error NotFound = Error.NotFound("Study.NotFound", "Study was not found.");
    public static Error StudyNotFoundById(string id) => Error.NotFound("Study.NotFoundById", $"Study with ID '{id}' was not found.");
    public static readonly Error InvalidUserId = Error.Validation("Study.InvalidUserId", "Invalid user ID.");
    public static readonly Error InvalidStatus = Error.Validation("Study.InvalidStatus", "Invalid study status.");
    public static readonly Error InvalidResearchField = Error.Validation("Study.InvalidResearchField", "Invalid research field.");
    public static readonly Error InvalidRole = Error.Validation("Study.InvalidRole", "Invalid study role.");
    public static readonly Error InvalidEmail = Error.Validation("Study.InvalidEmail", "Invalid email address.");

    // Title errors
    public static readonly Error TitleRequired = Error.Validation("Study.TitleRequired", "Study title is required.");
    public static Error TitleTooShort(int min) => Error.Validation("Study.TitleTooShort", $"Study title must be at least {min} characters.");
    public static Error TitleTooLong(int max) => Error.Validation("Study.TitleTooLong", $"Study title must not exceed {max} characters.");

    // Description errors
    public static Error DescriptionTooLong(int max) => Error.Validation("Study.DescriptionTooLong", $"Study description must not exceed {max} characters.");

    // Status errors
    public static Error InvalidStatusTransition(StudyStatus from, StudyStatus to) =>
        Error.Validation("Study.InvalidStatusTransition", $"Cannot transition from {from.Name} to {to.Name}.");

    // Member errors
    public static readonly Error UserAlreadyMember = Error.Conflict("Study.UserAlreadyMember", "User is already a member of this study.");
    public static readonly Error UserNotMember = Error.NotFound("Study.UserNotMember", "User is not a member of this study.");
    public static readonly Error NotAMember = Error.NotFound("Study.NotAMember", "You are not a member of this study.");
    public static readonly Error CannotRemoveOwner = Error.Validation("Study.CannotRemoveOwner", "Cannot remove the study owner.");
    public static readonly Error CannotChangeOwnerRole = Error.Validation("Study.CannotChangeOwnerRole", "Cannot change the owner's role.");
    public static readonly Error OwnerCannotLeave = Error.Validation("Study.OwnerCannotLeave", "Owner cannot leave the study. Transfer ownership first.");
    public static readonly Error NewOwnerMustBeAdmin = Error.Validation("Study.NewOwnerMustBeAdmin", "New owner must be an admin of the study.");

    // Permission errors
    public static readonly Error InsufficientPermissions = Error.Forbidden("Study.InsufficientPermissions", "You don't have permission to perform this action.");
    public static readonly Error OnlyOwnerCanDelete = Error.Forbidden("Study.OnlyOwnerCanDelete", "Only the owner can delete this study.");
    public static readonly Error OnlyOwnerCanTransfer = Error.Forbidden("Study.OnlyOwnerCanTransfer", "Only the owner can transfer ownership.");

    // Paper errors
    public static readonly Error PaperTitleRequired = Error.Validation("Study.PaperTitleRequired", "Paper title is required.");
    public static Error PaperTitleTooLong(int max) => Error.Validation("Study.PaperTitleTooLong", $"Paper title must not exceed {max} characters.");
    public static readonly Error PaperNotFound = Error.NotFound("Study.PaperNotFound", "Paper was not found in this study.");
    public static readonly Error MaxPapersReached = Error.Validation("Study.MaxPapersReached", "Maximum number of papers reached.");

    // Star errors
    public static readonly Error AlreadyStarred = Error.Conflict("Study.AlreadyStarred", "You have already starred this study.");
    public static readonly Error NotStarred = Error.NotFound("Study.NotStarred", "You have not starred this study.");

    // Featured errors
    public static readonly Error OnlyAdminCanFeature = Error.Forbidden("Study.OnlyAdminCanFeature", "Only administrators can feature studies.");
    public static readonly Error AlreadyFeatured = Error.Conflict("Study.AlreadyFeatured", "Study is already featured.");
    public static readonly Error NotFeatured = Error.Conflict("Study.NotFeatured", "Study is not featured.");

    // Tag errors
    public static Error MaxTagsReached(int max) => Error.Validation("Study.MaxTagsReached", $"Maximum of {max} tags allowed.");
    public static Error TagTooLong(int max) => Error.Validation("Study.TagTooLong", $"Tag must not exceed {max} characters.");
    public static readonly Error TagAlreadyExists = Error.Conflict("Study.TagAlreadyExists", "This tag already exists on the study.");
    public static readonly Error TagNotFound = Error.NotFound("Study.TagNotFound", "Tag was not found on this study.");

    // Institution/PI errors
    public static Error InstitutionTooLong(int max) => Error.Validation("Study.InstitutionTooLong", $"Institution must not exceed {max} characters.");
    public static Error PrincipalInvestigatorTooLong(int max) => Error.Validation("Study.PrincipalInvestigatorTooLong", $"Principal investigator must not exceed {max} characters.");

    // README errors
    public static Error ReadmeTooLong(int max) => Error.Validation("Study.ReadmeTooLong", $"README must not exceed {max} characters.");

    // Invitation errors
    public static readonly Error InvitationNotFound = Error.NotFound("Study.InvitationNotFound", "Invitation was not found.");
    public static Error InvitationNotFoundByToken(string token) => Error.NotFound("Study.InvitationNotFoundByToken", $"Invitation with token '{token}' was not found.");
    public static readonly Error InvitationExpired = Error.Validation("Study.InvitationExpired", "This invitation has expired.");
    public static readonly Error InvitationAlreadyResponded = Error.Validation("Study.InvitationAlreadyResponded", "This invitation has already been responded to.");
    public static readonly Error InvitationAlreadyExists = Error.Conflict("Study.InvitationAlreadyExists", "An invitation for this email already exists.");
    public static readonly Error UserAlreadyInvited = Error.Conflict("Study.UserAlreadyInvited", "User has already been invited to this study.");
    public static readonly Error CannotInviteMember = Error.Conflict("Study.CannotInviteMember", "User is already a member of this study.");

    // Org-owner errors (when creating studies under an Organisation)
    public static readonly Error OrgOwnerHandleRequired = Error.Validation(
        "Study.OrgOwnerHandleRequired",
        "An organisation handle is required when creating a study owned by an org.");
    public static readonly Error OrgOwnerNotFound = Error.NotFound(
        "Study.OrgOwnerNotFound",
        "The organisation that should own this study was not found.");
    public static readonly Error OrgOwnerInsufficientRole = Error.Forbidden(
        "Study.OrgOwnerInsufficientRole",
        "You must be an Owner or Admin of the organisation to create a study under it.");
}
