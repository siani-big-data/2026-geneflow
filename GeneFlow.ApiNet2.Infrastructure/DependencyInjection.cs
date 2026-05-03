using Minio;
using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Services;
using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Infrastructure.Analysis;
using GeneFlow.ApiNet2.Infrastructure.Events;
using GeneFlow.ApiNet2.Infrastructure.Jobs;
using GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Repositories;
using GeneFlow.ApiNet2.Infrastructure.Identity.Services;
using GeneFlow.ApiNet2.Infrastructure.Identity.Services.OAuth;
using GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Repositories;
using GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Persistence.Repositories;
using GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Services;
using GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Repositories;
using GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Repositories;
using GeneFlow.ApiNet2.Infrastructure.Redis;
using GeneFlow.ApiNet2.Infrastructure.Redis.Configuration;
using GeneFlow.ApiNet2.Infrastructure.Storage;
using GeneFlow.ApiNet2.Infrastructure.Storage.Configuration;
using GeneFlow.ApiNet2.Infrastructure.Storage.Services;
using GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Repositories;
using GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Repositories;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Repositories;
using GeneFlow.ApiNet2.Infrastructure.Traces.Services;
using GeneFlow.ApiNet2.Infrastructure.Usage.Repositories;
using GeneFlow.ApiNet2.Infrastructure.Usage.Services;
using GeneFlow.ApiNet2.Infrastructure.Services;
using GeneFlow.ApiNet2.Domain.Usage;
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
    /// Maximum number of times Npgsql will retry a failed command before
    /// surfacing the exception to the caller.
    /// </summary>
    private const int DbMaxRetryCount = 3;

    /// <summary>
    /// Upper bound on the back-off delay between Npgsql retry attempts.
    /// </summary>
    private static readonly TimeSpan DbMaxRetryDelay = TimeSpan.FromSeconds(5);

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
        services.AddStorageServices(configuration);

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
                    maxRetryCount: DbMaxRetryCount,
                    maxRetryDelay: DbMaxRetryDelay,
                    errorCodesToAdd: null);
            }));

        // Identity Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserUnitOfWork, UserUnitOfWork>();

        // Profiles DbContext
        services.AddDbContext<ProfileContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ProfileContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: DbMaxRetryCount,
                    maxRetryDelay: DbMaxRetryDelay,
                    errorCodesToAdd: null);
            }));

        // Profiles Repositories
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProfileUnitOfWork, ProfileUnitOfWork>();

        // Plans DbContext
        services.AddDbContext<PlanContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PlanContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: DbMaxRetryCount,
                    maxRetryDelay: DbMaxRetryDelay,
                    errorCodesToAdd: null);
            }));

        // Plans Repositories
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IPlanUnitOfWork, PlanUnitOfWork>();

        // Subscriptions DbContext
        services.AddDbContext<SubscriptionContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(SubscriptionContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: DbMaxRetryCount,
                    maxRetryDelay: DbMaxRetryDelay,
                    errorCodesToAdd: null);
            }));

        // Subscriptions Repositories
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionUnitOfWork, SubscriptionUnitOfWork>();

        // Studies DbContext
        services.AddDbContext<StudyContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(StudyContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: DbMaxRetryCount,
                    maxRetryDelay: DbMaxRetryDelay,
                    errorCodesToAdd: null);
            }));

        // Studies Repositories
        services.AddScoped<IStudyRepository, StudyRepository>();
        services.AddScoped<IStudyInvitationRepository, StudyInvitationRepository>();
        services.AddScoped<IStudyUnitOfWork, StudyUnitOfWork>();

        // Traces DbContext
        services.AddDbContext<TraceContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(TraceContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: DbMaxRetryCount,
                    maxRetryDelay: DbMaxRetryDelay,
                    errorCodesToAdd: null);
            }));

        // Traces Repositories
        services.AddScoped<ITraceRepository, TraceRepository>();
        services.AddScoped<ITraceUnitOfWork, TraceUnitOfWork>();

        // Traces Services
        services.AddScoped<ITraceAnalysisService, TraceAnalysisService>();

        // Pipelines DbContext
        services.AddDbContext<PipelineContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PipelineContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: DbMaxRetryCount,
                    maxRetryDelay: DbMaxRetryDelay,
                    errorCodesToAdd: null);
            }));

        // Pipelines Repositories
        services.AddScoped<IPipelineRepository, PipelineRepository>();
        services.AddScoped<IPipelineExecutionRepository, PipelineExecutionRepository>();
        services.AddScoped<IPipelineUnitOfWork, PipelineUnitOfWork>();

        // PaymentMethods DbContext
        services.AddDbContext<PaymentMethodContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PaymentMethodContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: DbMaxRetryCount,
                    maxRetryDelay: DbMaxRetryDelay,
                    errorCodesToAdd: null);
            }));

        // PaymentMethods Repositories
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();

        // Stripe Service
        services.AddSingleton<IStripeService, StripeService>();

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

        // Cache service (Redis-based)
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Usage statistics repository (Redis-based datamart)
        services.AddScoped<IUsageStatsRepository, RedisUsageStatsRepository>();

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

        services.Configure<TwoFactorSettings>(
            configuration.GetSection(TwoFactorSettings.SectionName));

        services.Configure<OAuthSettings>(
            configuration.GetSection(OAuthSettings.SectionName));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITwoFactorAuthenticator, TwoFactorAuthenticator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserAuthenticationValidator, UserAuthenticationValidator>();

        // OAuth services
        services.AddHttpClient<GoogleTokenValidator>();
        services.AddHttpClient<GitHubTokenValidator>();
        services.AddScoped<IOAuthTokenValidator, OAuthTokenValidator>();

        // Worker API key validation (for background processing services)
        services.AddScoped<IWorkerApiKeyValidator, WorkerApiKeyValidator>();

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
        services.AddSingleton<IEventBusSubscriber, RedisEventBusSubscriber>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Job publisher for Analysis worker
        services.AddSingleton<IJobPublisher, RedisJobPublisher>();

        // Usage stats event processor (background service)
        services.AddHostedService<UsageStatsEventProcessor>();

        // Analysis event processor (consumes events from Python Analysis worker)
        services.AddHostedService<AnalysisEventProcessor>();

        return services;
    }

    /// <summary>
    /// Adds storage services for the Datalake.
    /// </summary>
    private static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Image processing service
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();

        // Datalake Storage (MinIO/S3) for trace files and chunked data
        services.Configure<DatalakeStorageSettings>(
            configuration.GetSection(DatalakeStorageSettings.SectionName));

        var datalakeSettings = configuration
            .GetSection(DatalakeStorageSettings.SectionName)
            .Get<DatalakeStorageSettings>() ?? new DatalakeStorageSettings();

        // MinIO client configuration
        services.AddSingleton<IMinioClient>(sp =>
        {
            var client = new MinioClient()
                .WithEndpoint(datalakeSettings.EndpointUrl.Replace("http://", "").Replace("https://", ""))
                .WithCredentials(datalakeSettings.AccessKey, datalakeSettings.SecretKey)
                .WithSSL(datalakeSettings.UseSSL)
                .WithTimeout((int)TimeSpan.FromSeconds(datalakeSettings.TimeoutSeconds).TotalMilliseconds)
                .Build();

            return client;
        });

        // File storage uses MinIO for trace uploads
        services.AddSingleton<IFileStorageService, MinIOFileStorageService>();
        services.AddScoped<IDatalakeStorageClient, MinIODatalakeStorageClient>();

        return services;
    }
}
