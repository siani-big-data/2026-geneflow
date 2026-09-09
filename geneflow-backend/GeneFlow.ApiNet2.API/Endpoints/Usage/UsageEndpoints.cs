using GeneFlow.ApiNet2.API.Contracts.Usage.Requests;
using GeneFlow.ApiNet2.API.Contracts.Usage.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Usage.Queries.GetBillingUsage;
using GeneFlow.ApiNet2.Application.Usage.Queries.GetDashboardStats;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Usage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Usage;

/// <summary>
/// Endpoints for usage statistics.
/// </summary>
public sealed class UsageEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/usage")
            .WithTags("Usage")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/billing", GetBillingUsage)
            .WithName("Usage_GetBillingUsage")
            .WithSummary("Get billing usage statistics")
            .WithDescription("Returns the current user's usage statistics for billing purposes.")
            .Produces<BillingUsageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/dashboard", GetDashboardStats)
            .WithName("Usage_GetDashboardStats")
            .WithSummary("Get dashboard statistics")
            .WithDescription("Returns the current user's dashboard statistics.")
            .Produces<DashboardStatsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Admin endpoints for managing usage stats (e.g., migration, testing)
        var adminGroup = app.MapGroup("/api/v1/admin/usage")
            .WithTags("Usage Admin")
            .WithOpenApi()
            .RequireAuthorization("Admin"); // Requires admin role

        adminGroup.MapPost("/increment/{userId}", IncrementUsageCounter)
            .WithName("Usage_IncrementCounter")
            .WithSummary("Increment a usage counter for a user")
            .WithDescription("Increments a specific usage counter. Requires admin role.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        adminGroup.MapPost("/sync/{userId}", SyncUsageStats)
            .WithName("Usage_SyncStats")
            .WithSummary("Sync usage stats for a user")
            .WithDescription("Updates all usage stats for a user. Requires admin role.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        adminGroup.MapPost("/init/{userId}", InitializeUsageStats)
            .WithName("Usage_InitStats")
            .WithSummary("Initialize usage stats for a user")
            .WithDescription("Creates initial usage stats for a user. Requires admin role.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetBillingUsage(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetBillingUsageQuery(currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var dto = result.Value;
        var response = new BillingUsageResponse(
            Studies: new UsageItemResponse(dto.Studies.Used, dto.Studies.Total, dto.Studies.Percentage),
            Traces: new UsageItemResponse(dto.Traces.Used, dto.Traces.Total, dto.Traces.Percentage),
            Members: new UsageItemResponse(dto.Members.Used, dto.Members.Total, dto.Members.Percentage),
            Period: new BillingPeriodResponse(
                dto.Period.StartDate,
                dto.Period.EndDate,
                dto.Period.DaysRemaining,
                dto.Period.TotalDays));

        return Results.Ok(response);
    }

    private static async Task<IResult> GetDashboardStats(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetDashboardStatsQuery(currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var dto = result.Value;
        var response = new DashboardStatsResponse(
            dto.ActiveStudies,
            dto.ProcessedTraces,
            dto.PendingTraces,
            dto.TeamActivity,
            dto.AlignmentsCompleted);

        return Results.Ok(response);
    }

    private static async Task<IResult> IncrementUsageCounter(
        string userId,
        [FromBody] IncrementUsageRequest request,
        [FromServices] IUsageStatsRepository repository,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(userId, out var parsedUserId) || parsedUserId is null)
            return Results.BadRequest("Invalid user ID format");

        var counterType = request.CounterType.ToLowerInvariant();
        var amount = request.Amount;

        switch (counterType)
        {
            case "studies_owned":
                for (var i = 0; i < amount; i++)
                    await repository.IncrementStudiesOwnedAsync(parsedUserId, cancellationToken);
                break;

            case "studies_total":
                for (var i = 0; i < amount; i++)
                    await repository.IncrementStudiesTotalAsync(parsedUserId, cancellationToken);
                break;

            case "traces":
                await repository.IncrementTracesAsync(parsedUserId, amount, cancellationToken);
                break;

            case "alignments":
                for (var i = 0; i < amount; i++)
                    await repository.IncrementAlignmentsAsync(parsedUserId, cancellationToken);
                break;

            case "alignments_completed":
                for (var i = 0; i < amount; i++)
                    await repository.IncrementCompletedAlignmentsAsync(parsedUserId, cancellationToken);
                break;

            default:
                return Results.BadRequest($"Unknown counter type: {request.CounterType}");
        }

        return Results.NoContent();
    }

    private static async Task<IResult> SyncUsageStats(
        string userId,
        [FromBody] UpdateUsageStatsRequest request,
        [FromServices] IUsageStatsRepository repository,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(userId, out var parsedUserId) || parsedUserId is null)
            return Results.BadRequest("Invalid user ID format");

        // Get existing stats or create new
        var stats = await repository.GetByUserIdAsync(parsedUserId, cancellationToken);
        if (stats is null)
        {
            stats = UsageStats.Create(parsedUserId, BillingPeriodKey.Current());
        }

        // Update with provided values
        stats.SetStats(
            studiesOwned: request.StudiesOwned ?? stats.StudiesOwned,
            studiesTotal: request.StudiesTotal ?? stats.StudiesTotal,
            tracesThisPeriod: request.TracesThisPeriod ?? stats.TracesThisPeriod,
            tracesTotal: request.TracesTotal ?? stats.TracesTotal,
            maxMembersInStudy: request.MaxMembersInStudy ?? stats.MaxMembersInStudy,
            alignmentsThisPeriod: request.AlignmentsThisPeriod ?? stats.AlignmentsThisPeriod,
            alignmentsTotal: request.AlignmentsTotal ?? stats.AlignmentsTotal,
            alignmentsCompleted: request.AlignmentsCompleted ?? stats.AlignmentsCompleted,
            tracesPending: request.TracesPending ?? stats.TracesPending,
            lastActivityAt: DateTime.UtcNow);

        await repository.SaveAsync(stats, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> InitializeUsageStats(
        string userId,
        [FromServices] IUsageStatsRepository repository,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(userId, out var parsedUserId) || parsedUserId is null)
            return Results.BadRequest("Invalid user ID format");

        // Check if stats already exist
        var existing = await repository.GetByUserIdAsync(parsedUserId, cancellationToken);
        if (existing is not null)
            return Results.Ok("Stats already exist for this user");

        // Create initial stats
        var stats = UsageStats.Create(parsedUserId, BillingPeriodKey.Current());
        await repository.SaveAsync(stats, cancellationToken);

        return Results.NoContent();
    }
}
