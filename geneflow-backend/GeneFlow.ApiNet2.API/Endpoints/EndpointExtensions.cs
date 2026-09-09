using System.Reflection;

namespace GeneFlow.ApiNet2.API.Endpoints;

/// <summary>
/// Extension methods for endpoint registration.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Discovers and maps all IEndpoint implementations in the specified assembly.
    /// </summary>
    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();

        var endpointTypes = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                        typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
