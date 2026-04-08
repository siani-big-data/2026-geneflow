using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Plans.Enumerations;

/// <summary>
/// Enumeration for plan features.
/// </summary>
public sealed class PlanFeature : Enumeration<PlanFeature>
{
    /// <summary>Access to AI Copilot features.</summary>
    public static readonly PlanFeature CopilotAccess = new(1, nameof(CopilotAccess));

    /// <summary>Priority processing for traces and alignments.</summary>
    public static readonly PlanFeature PriorityProcessing = new(2, nameof(PriorityProcessing));

    /// <summary>Advanced analytics and reporting.</summary>
    public static readonly PlanFeature AdvancedAnalytics = new(3, nameof(AdvancedAnalytics));

    /// <summary>API access for integrations.</summary>
    public static readonly PlanFeature ApiAccess = new(4, nameof(ApiAccess));

    /// <summary>Export features (PDF, CSV, etc.).</summary>
    public static readonly PlanFeature ExportFeatures = new(5, nameof(ExportFeatures));

    /// <summary>Team collaboration features.</summary>
    public static readonly PlanFeature TeamCollaboration = new(6, nameof(TeamCollaboration));

    /// <summary>Custom workflows.</summary>
    public static readonly PlanFeature CustomWorkflows = new(7, nameof(CustomWorkflows));

    /// <summary>SSO and SAML integration.</summary>
    public static readonly PlanFeature SsoIntegration = new(8, nameof(SsoIntegration));

    /// <summary>Dedicated support.</summary>
    public static readonly PlanFeature DedicatedSupport = new(9, nameof(DedicatedSupport));

    private PlanFeature(int id, string name) : base(id, name)
    {
    }
}
