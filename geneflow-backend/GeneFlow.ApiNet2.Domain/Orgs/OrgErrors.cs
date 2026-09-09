using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Orgs;

public static class OrgErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Org.NotFound", "Organisation was not found.");

    public static readonly Error HandleRequired =
        Error.Validation("Org.HandleRequired", "Organisation handle is required.");

    public static readonly Error HandleInvalid =
        Error.Validation(
            "Org.HandleInvalid",
            "Organisation handle must be 2-39 characters of lowercase letters, digits or hyphens.");

    public static readonly Error HandleTaken =
        Error.Conflict("Org.HandleTaken", "That organisation handle is already taken.");

    public static readonly Error NameRequired =
        Error.Validation("Org.NameRequired", "Organisation name is required.");

    public static readonly Error MemberNotFound =
        Error.NotFound("Org.MemberNotFound", "That user is not a member of the organisation.");

    public static readonly Error MustHaveOwner =
        Error.Validation(
            "Org.MustHaveOwner",
            "An organisation must always have at least one Owner.");

    public static readonly Error Forbidden =
        Error.Forbidden("Org.Forbidden", "You do not have permission to perform this action.");

    public static readonly Error AlreadyMember =
        Error.Conflict("Org.AlreadyMember", "That user is already a member of the organisation.");

    public static class OrgInvitation
    {
        public static readonly Error NotFound =
            Error.NotFound("OrgInvitation.NotFound", "Invitation was not found.");

        public static readonly Error NotPending =
            Error.Validation("OrgInvitation.NotPending", "Invitation is not pending.");

        public static readonly Error Expired =
            Error.Validation("OrgInvitation.Expired", "Invitation has expired.");

        public static readonly Error WrongUser =
            Error.Forbidden(
                "OrgInvitation.WrongUser",
                "This invitation was sent to a different user.");

        public static readonly Error EmailRequired =
            Error.Validation("OrgInvitation.EmailRequired", "Invitation email is required.");
    }
}
