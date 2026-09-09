using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;

namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates worker API key for requests
/// that implement <see cref="IRequireWorkerApiKey"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class WorkerApiKeyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequireWorkerApiKey
{
    private readonly IWorkerApiKeyValidator _apiKeyValidator;

    public WorkerApiKeyBehavior(IWorkerApiKeyValidator apiKeyValidator)
    {
        _apiKeyValidator = apiKeyValidator;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_apiKeyValidator.IsValidApiKey)
        {
            return CreateUnauthorizedResult();
        }

        return await next();
    }

    private static TResponse CreateUnauthorizedResult()
    {
        var error = Error.Unauthorized("Worker.InvalidApiKey", "Invalid or missing worker API key.");

        // Handle Result<TValue> responses
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure), new[] { typeof(Error) });

            return (TResponse)failureMethod!.Invoke(null, new object[] { error })!;
        }

        // Handle plain Result responses
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        throw new InvalidOperationException(
            $"WorkerApiKeyBehavior does not support response type {typeof(TResponse).Name}. " +
            "Only Result and Result<T> are supported.");
    }
}
