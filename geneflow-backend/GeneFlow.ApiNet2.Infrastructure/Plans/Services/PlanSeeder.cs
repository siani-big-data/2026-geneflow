using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.Enumerations;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;
using GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Plans.Services;

/// <summary>
/// Seeds default plans into the database.
/// </summary>
public sealed class PlanSeeder
{
    private readonly PlanContext _context;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly ILogger<PlanSeeder> _logger;

    public PlanSeeder(
        PlanContext context,
        ISequenceGenerator sequenceGenerator,
        ILogger<PlanSeeder> logger)
    {
        _context = context;
        _sequenceGenerator = sequenceGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the default plans if they don't exist.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if plans already exist
        if (await _context.Plans.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Plans already exist, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding default plans...");

        var plans = await CreateDefaultPlansAsync(cancellationToken);

        foreach (var plan in plans)
        {
            await _context.Plans.AddAsync(plan, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} plans successfully", plans.Count);
    }

    private async Task<List<Plan>> CreateDefaultPlansAsync(CancellationToken cancellationToken)
    {
        var plans = new List<Plan>();

        // Free Plan (L00000001)
        var freeId = PlanId.FromSequence(await _sequenceGenerator.NextAsync(PlanId.SequenceName, cancellationToken));
        var freePlan = Plan.Create(
            freeId,
            PlanName.Create("Free").Value,
            "Plan gratuito para empezar. Ideal para probar la plataforma.",
            PlanPricing.Free(),
            PlanLimits.FreeTier(),
            displayOrder: 0,
            isDefault: true).Value;
        plans.Add(freePlan);

        // Pro Plan (L00000002)
        var proId = PlanId.FromSequence(await _sequenceGenerator.NextAsync(PlanId.SequenceName, cancellationToken));
        var proPlan = Plan.Create(
            proId,
            PlanName.Create("Pro").Value,
            "Para investigadores individuales y equipos pequenos.",
            PlanPricing.Create(29m, 290m, "EUR").Value,
            PlanLimits.Create(10, 500, 10).Value,
            displayOrder: 1).Value;

        proPlan.AddFeature(PlanFeature.CopilotAccess);
        proPlan.AddFeature(PlanFeature.PriorityProcessing);
        proPlan.AddFeature(PlanFeature.ExportFeatures);
        plans.Add(proPlan);

        // Enterprise Plan (L00000003)
        var enterpriseId = PlanId.FromSequence(await _sequenceGenerator.NextAsync(PlanId.SequenceName, cancellationToken));
        var enterprisePlan = Plan.Create(
            enterpriseId,
            PlanName.Create("Enterprise").Value,
            "Para equipos grandes y organizaciones. Sin limites.",
            PlanPricing.Create(99m, 990m, "EUR").Value,
            PlanLimits.CreateUnlimited(),
            displayOrder: 2).Value;

        enterprisePlan.AddFeature(PlanFeature.CopilotAccess);
        enterprisePlan.AddFeature(PlanFeature.PriorityProcessing);
        enterprisePlan.AddFeature(PlanFeature.AdvancedAnalytics);
        enterprisePlan.AddFeature(PlanFeature.ApiAccess);
        enterprisePlan.AddFeature(PlanFeature.ExportFeatures);
        enterprisePlan.AddFeature(PlanFeature.TeamCollaboration);
        enterprisePlan.AddFeature(PlanFeature.CustomWorkflows);
        enterprisePlan.AddFeature(PlanFeature.SsoIntegration);
        enterprisePlan.AddFeature(PlanFeature.DedicatedSupport);
        plans.Add(enterprisePlan);

        return plans;
    }
}
