namespace GeneFlow.ApiNet2.SharedKernel.Application.CQRS;

/// <summary>
/// Marker interface for queries.
/// Queries represent requests for data without side effects.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;
