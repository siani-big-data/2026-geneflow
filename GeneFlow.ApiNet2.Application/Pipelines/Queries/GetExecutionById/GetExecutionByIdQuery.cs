using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetExecutionById;

/// <summary>
/// Query to get an execution by ID.
/// </summary>
public sealed record GetExecutionByIdQuery(
    string UserId,
    string ExecutionId) : IQuery<Result<PipelineExecutionDto>>, IRequireAuthentication;
