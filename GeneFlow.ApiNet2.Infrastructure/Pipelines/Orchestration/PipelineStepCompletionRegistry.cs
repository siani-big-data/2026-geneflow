using System.Collections.Concurrent;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Orchestration;

/// <summary>
/// Thread-safe singleton registry that lets the pipeline orchestrator wait for
/// step-completion events emitted by the analysis worker. Keyed by
/// <c>{traceId}:{analysisType}</c>; pipeline executions are sequential per trace
/// (the domain enforces a single in-flight execution per trace), so this key is
/// unique at any moment.
/// </summary>
public sealed class PipelineStepCompletionRegistry
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<StepResult>> _waiters = new();

    /// <summary>
    /// Registers a waiter for the given (trace, analysisType) pair and returns a
    /// task that resolves when <see cref="Complete"/> is called or the cancellation
    /// token fires. Throws if a waiter is already registered for the same key.
    /// </summary>
    public Task<StepResult> WaitAsync(string traceId, string analysisType, CancellationToken cancellationToken)
    {
        var key = MakeKey(traceId, analysisType);
        var tcs = new TaskCompletionSource<StepResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_waiters.TryAdd(key, tcs))
        {
            throw new InvalidOperationException(
                $"A waiter is already registered for key '{key}'. Pipelines must be sequential per trace.");
        }

        var registration = cancellationToken.Register(() =>
        {
            if (_waiters.TryRemove(key, out var existing))
            {
                existing.TrySetCanceled(cancellationToken);
            }
        });

        // Ensure the registration is disposed once the task settles.
        tcs.Task.ContinueWith(
            _ => registration.Dispose(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return tcs.Task;
    }

    /// <summary>
    /// Resolves a registered waiter for the given (trace, analysisType) pair.
    /// Returns <c>true</c> if a waiter was found and notified, <c>false</c>
    /// otherwise (event not for the orchestrator).
    /// </summary>
    public bool Complete(string traceId, string analysisType, bool success, string? error = null)
    {
        var key = MakeKey(traceId, analysisType);
        if (_waiters.TryRemove(key, out var tcs))
        {
            tcs.TrySetResult(success ? StepResult.Ok() : StepResult.Fail(error));
            return true;
        }
        return false;
    }

    private static string MakeKey(string traceId, string analysisType)
        => $"{traceId}:{analysisType.ToLowerInvariant()}";
}
