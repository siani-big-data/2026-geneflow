using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetInvitationByToken;

/// <summary>
/// Query to get an invitation by its token.
/// </summary>
public sealed record GetInvitationByTokenQuery(string Token) : IQuery<Result<StudyInvitationDto>>;
