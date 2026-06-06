using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Commands.ReindexAll;

/// <summary>
/// One-shot backfill that re-projects every study and discussion into the
/// search index. Idempotent — existing entries get updated, missing ones
/// get inserted. Intended to be triggered manually after the search feature
/// is rolled out or after a schema change.
/// </summary>
public sealed record ReindexAllCommand : ICommand<Result<ReindexAllResult>>;

/// <summary>Counts returned by <see cref="ReindexAllCommand"/>.</summary>
public sealed record ReindexAllResult(int StudiesIndexed, int DiscussionsIndexed);
