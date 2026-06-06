using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Commands.RemoveFromIndex;

/// <summary>
/// Removes an object from the search index. No-op if not indexed.
/// </summary>
public sealed record RemoveFromIndexCommand(
    string ObjectType,
    string ObjectId) : ICommand<Result>;
