using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Commands.IndexObject;

/// <summary>
/// Upserts (creates or updates) a search index entry for a domain object.
/// Idempotent — re-running with the same (objectType, objectId) replaces
/// the previous projection.
/// </summary>
public sealed record IndexObjectCommand(
    string ObjectType,
    string ObjectId,
    string? OwnerId,
    string Title,
    string? Body,
    string? Tags,
    bool IsPublic) : ICommand<Result>;
