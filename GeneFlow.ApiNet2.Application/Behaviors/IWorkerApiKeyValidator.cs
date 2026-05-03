namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// Service interface for validating worker API keys.
/// </summary>
public interface IWorkerApiKeyValidator
{
    /// <summary>
    /// Gets whether a valid worker API key is present in the current request.
    /// </summary>
    bool IsValidApiKey { get; }
}
