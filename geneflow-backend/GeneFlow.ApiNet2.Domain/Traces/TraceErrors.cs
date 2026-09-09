using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// Domain errors for the Trace aggregate. Errors are split across partials by
/// subdomain to keep each file focused and within the project size guideline.
/// This file groups general, identifier, name and description errors. Other partials:
/// <see cref="TraceErrors"/> in <c>TraceErrors.File.cs</c>,
/// <c>TraceErrors.Editing.cs</c> and <c>TraceErrors.Processing.cs</c>.
/// </summary>
public static partial class TraceErrors
{
    /// <summary>Trace was not found.</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Trace.NotFound", "Trace was not found.");

    /// <summary>Trace not found by id.</summary>
    public static Error TraceNotFoundById(string id) => Error.NotFound(
        "Trace.NotFoundById", $"Trace with ID '{id}' was not found.");

    /// <summary>Invalid trace identifier.</summary>
    public static readonly Error InvalidTraceId = Error.Validation(
        "Trace.InvalidTraceId", "Invalid trace ID.");

    /// <summary>Invalid user identifier.</summary>
    public static readonly Error InvalidUserId = Error.Validation(
        "Trace.InvalidUserId", "Invalid user ID.");

    /// <summary>Invalid study identifier.</summary>
    public static readonly Error InvalidStudyId = Error.Validation(
        "Trace.InvalidStudyId", "Invalid study ID.");

    /// <summary>Trace name is required.</summary>
    public static readonly Error NameRequired = Error.Validation(
        "Trace.NameRequired", "Trace name is required.");

    /// <summary>Trace name is too short.</summary>
    public static Error NameTooShort(int min) => Error.Validation(
        "Trace.NameTooShort", $"Trace name must be at least {min} characters.");

    /// <summary>Trace name is too long.</summary>
    public static Error NameTooLong(int max) => Error.Validation(
        "Trace.NameTooLong", $"Trace name must not exceed {max} characters.");

    /// <summary>Trace description is too long.</summary>
    public static Error DescriptionTooLong(int max) => Error.Validation(
        "Trace.DescriptionTooLong", $"Trace description must not exceed {max} characters.");

    /// <summary>Insufficient permissions for trace operation.</summary>
    public static readonly Error InsufficientPermissions = Error.Forbidden(
        "Trace.InsufficientPermissions", "You don't have permission to perform this action.");
}
