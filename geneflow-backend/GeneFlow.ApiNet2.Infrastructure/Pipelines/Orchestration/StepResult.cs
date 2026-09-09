namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Orchestration;

/// <summary>
/// Outcome of a single pipeline step awaited by the orchestrator.
/// </summary>
/// <param name="Success">True if the worker reported successful completion.</param>
/// <param name="Error">Error message when <see cref="Success"/> is false; null otherwise.</param>
public sealed record StepResult(bool Success, string? Error)
{
    public static StepResult Ok() => new(true, null);
    public static StepResult Fail(string? error) => new(false, error);
}
