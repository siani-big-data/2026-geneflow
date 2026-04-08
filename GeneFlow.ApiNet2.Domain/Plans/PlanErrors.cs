using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Plans;

/// <summary>
/// Domain errors for Plan aggregate.
/// </summary>
public static class PlanErrors
{
    /// <summary>Plan was not found.</summary>
    public static Error NotFound => Error.NotFound(
        "Plan.NotFound",
        "The specified plan was not found.");

    /// <summary>Plan name is required.</summary>
    public static Error NameRequired => Error.Validation(
        "Plan.NameRequired",
        "Plan name is required.");

    /// <summary>Plan name is too short.</summary>
    public static Error NameTooShort(int minLength) => Error.Validation(
        "Plan.NameTooShort",
        $"Plan name must be at least {minLength} characters.");

    /// <summary>Plan name is too long.</summary>
    public static Error NameTooLong(int maxLength) => Error.Validation(
        "Plan.NameTooLong",
        $"Plan name cannot exceed {maxLength} characters.");

    /// <summary>Plan name already exists.</summary>
    public static Error NameAlreadyExists => Error.Conflict(
        "Plan.NameAlreadyExists",
        "A plan with this name already exists.");

    /// <summary>Invalid monthly price.</summary>
    public static Error InvalidMonthlyPrice => Error.Validation(
        "Plan.InvalidMonthlyPrice",
        "Monthly price cannot be negative.");

    /// <summary>Invalid annual price.</summary>
    public static Error InvalidAnnualPrice => Error.Validation(
        "Plan.InvalidAnnualPrice",
        "Annual price cannot be negative.");

    /// <summary>Invalid currency code.</summary>
    public static Error InvalidCurrency => Error.Validation(
        "Plan.InvalidCurrency",
        "Currency must be a valid 3-letter ISO 4217 code.");

    /// <summary>Invalid max studies limit.</summary>
    public static Error InvalidMaxStudies => Error.Validation(
        "Plan.InvalidMaxStudies",
        "Max studies must be -1 (unlimited) or a positive number.");

    /// <summary>Invalid max traces limit.</summary>
    public static Error InvalidMaxTraces => Error.Validation(
        "Plan.InvalidMaxTraces",
        "Max traces per month must be -1 (unlimited) or a positive number.");

    /// <summary>Invalid max members limit.</summary>
    public static Error InvalidMaxMembers => Error.Validation(
        "Plan.InvalidMaxMembers",
        "Max members per study must be -1 (unlimited) or a positive number.");

    /// <summary>Cannot deactivate default plan.</summary>
    public static Error CannotDeactivateDefaultPlan => Error.Validation(
        "Plan.CannotDeactivateDefaultPlan",
        "Cannot deactivate the default plan. Set another plan as default first.");

    /// <summary>Plan is not active.</summary>
    public static Error NotActive => Error.Validation(
        "Plan.NotActive",
        "The plan is not active.");

    /// <summary>Feature already exists in plan.</summary>
    public static Error FeatureAlreadyExists => Error.Conflict(
        "Plan.FeatureAlreadyExists",
        "This feature is already included in the plan.");

    /// <summary>Feature not found in plan.</summary>
    public static Error FeatureNotFound => Error.NotFound(
        "Plan.FeatureNotFound",
        "This feature is not included in the plan.");
}
