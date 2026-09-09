namespace GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;

/// <summary>
/// The type of audit action.
/// </summary>
public enum AuditAction
{
    /// <summary>Entity was created.</summary>
    Created = 1,

    /// <summary>Entity was modified.</summary>
    Modified = 2,

    /// <summary>Entity was deleted.</summary>
    Deleted = 3,

    /// <summary>Entity was restored from deletion.</summary>
    Restored = 4
}
