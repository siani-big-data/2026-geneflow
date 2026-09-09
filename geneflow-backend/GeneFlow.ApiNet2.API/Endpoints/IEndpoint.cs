namespace GeneFlow.ApiNet2.API.Endpoints;

/// <summary>
/// Interface for defining API endpoint groups.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps the endpoint routes to the application.
    /// </summary>
    void MapEndpoint(IEndpointRouteBuilder app);
}
