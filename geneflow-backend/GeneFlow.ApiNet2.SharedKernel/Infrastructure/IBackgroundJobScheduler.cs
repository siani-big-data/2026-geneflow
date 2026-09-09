using System.Linq.Expressions;

namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Abstraction for background job scheduling (Hangfire, Quartz, etc.).
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// Enqueues a job for immediate execution.
    /// </summary>
    /// <returns>The job identifier.</returns>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Schedules a job for delayed execution.
    /// </summary>
    /// <returns>The job identifier.</returns>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a job for execution at a specific time.
    /// </summary>
    /// <returns>The job identifier.</returns>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTime scheduledTime);

    /// <summary>
    /// Creates or updates a recurring job.
    /// </summary>
    /// <param name="jobId">Unique identifier for the recurring job.</param>
    /// <param name="methodCall">The method to execute.</param>
    /// <param name="cronExpression">Cron expression for the schedule.</param>
    void AddOrUpdateRecurring<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);

    /// <summary>
    /// Removes a recurring job.
    /// </summary>
    void RemoveRecurring(string jobId);

    /// <summary>
    /// Triggers a recurring job immediately.
    /// </summary>
    void TriggerRecurring(string jobId);

    /// <summary>
    /// Continues with another job after the specified job completes.
    /// </summary>
    /// <param name="parentJobId">The job to wait for.</param>
    /// <param name="methodCall">The method to execute after parent completes.</param>
    /// <returns>The continuation job identifier.</returns>
    string ContinueWith<T>(string parentJobId, Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Deletes a scheduled or enqueued job.
    /// </summary>
    /// <returns>True if the job was deleted.</returns>
    bool Delete(string jobId);
}
