using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Services;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Infrastructure.Events;
using GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Repositories;
using GeneFlow.ApiNet2.Infrastructure.Identity.Services;
using GeneFlow.ApiNet2.Infrastructure.Redis;
using GeneFlow.ApiNet2.Infrastructure.Redis.Configuration;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all Infrastructure layer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddRedis(configuration);
        services.AddIdentityServices(configuration);
        services.AddEventDispatching();

        return services;
    }

    /// <summary>
    /// Adds Entity Framework persistence services.
    /// </summary>
    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Identity DbContext
        services.AddDbContext<UserContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(UserContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        // Identity Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserUnitOfWork, UserUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds Redis services for caching and sequence generation.
    /// </summary>
    private static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisSettings>(
            configuration.GetSection(RedisSettings.SectionName));

        var redisSettings = configuration
            .GetSection(RedisSettings.SectionName)
            .Get<RedisSettings>() ?? new RedisSettings();

        var configurationOptions = ConfigurationOptions.Parse(redisSettings.ConnectionString);
        configurationOptions.ConnectTimeout = redisSettings.ConnectTimeoutMs;
        configurationOptions.SyncTimeout = redisSettings.SyncTimeoutMs;
        configurationOptions.AbortOnConnectFail = false;

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configurationOptions));

        services.AddSingleton<ISequenceGenerator, RedisSequenceGenerator>();

        return services;
    }

    /// <summary>
    /// Adds Identity-related services (auth, password hashing, etc.).
    /// </summary>
    private static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        services.Configure<EmailSettings>(
            configuration.GetSection(EmailSettings.SectionName));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserAuthenticationValidator, UserAuthenticationValidator>();

        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Adds domain event dispatching services.
    /// </summary>
    private static IServiceCollection AddEventDispatching(this IServiceCollection services)
    {
        services.AddSingleton<IEventCategoryResolver, EventCategoryResolver>();
        services.AddSingleton<IEventBusPublisher, RedisEventBusPublisher>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
