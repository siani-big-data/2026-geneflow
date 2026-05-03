namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// Marker interface for commands that require worker API key authentication.
/// These commands are typically called by background processing services
/// and require a valid API key in the X-Worker-Api-Key header.
/// </summary>
public interface IRequireWorkerApiKey
{
}
