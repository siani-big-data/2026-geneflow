using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Plans.Services;
using GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Database setup extensions for the application.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies all pending migrations and seeds the database.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var env = app.Environment;
        if (env.EnvironmentName == "Testing")
        {
            logger.LogInformation("Testing environment detected, skipping migrations");
            return;
        }

        try
        {
            logger.LogInformation("Applying database migrations...");

            var userContext = scope.ServiceProvider.GetRequiredService<UserContext>();
            await userContext.Database.MigrateAsync();
            logger.LogInformation("UserContext migrations applied");

            var profileContext = scope.ServiceProvider.GetRequiredService<ProfileContext>();
            await profileContext.Database.MigrateAsync();
            logger.LogInformation("ProfileContext migrations applied");

            var planContext = scope.ServiceProvider.GetRequiredService<PlanContext>();
            await planContext.Database.MigrateAsync();
            logger.LogInformation("PlanContext migrations applied");

            var subscriptionContext = scope.ServiceProvider.GetRequiredService<SubscriptionContext>();
            await subscriptionContext.Database.MigrateAsync();
            logger.LogInformation("SubscriptionContext migrations applied");

            var planSeeder = new PlanSeeder(
                planContext,
                scope.ServiceProvider.GetRequiredService<ISequenceGenerator>(),
                scope.ServiceProvider.GetRequiredService<ILogger<PlanSeeder>>());
            await planSeeder.SeedAsync();

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }
}
