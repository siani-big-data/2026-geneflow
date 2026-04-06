using GeneFlow.ApiNet2.API.Middleware;
using GeneFlow.ApiNet2.Infrastructure;
using Microsoft.OpenApi.Models;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Extension methods for configuring API services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all API-related services to the service collection.
    /// </summary>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(Application.Identity.Commands.Register.RegisterUserCommand).Assembly);
        });

        // Add Infrastructure services
        services.AddInfrastructure(configuration);

        // Add JWT Authentication
        services.AddJwtAuthentication(configuration);

        // Add exception handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Add Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "GeneFlow API",
                Version = "v1",
                Description = "GeneFlow Backend API for genetic sequence analysis"
            });

            // Add JWT support in Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
