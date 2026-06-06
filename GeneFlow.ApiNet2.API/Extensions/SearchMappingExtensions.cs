using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Search.Responses;
using GeneFlow.ApiNet2.Application.Search.Common;
using GeneFlow.ApiNet2.Application.Search.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Maps Application Search DTOs onto API contract responses.
/// </summary>
public static class SearchMappingExtensions
{
    public static SearchHitResponse ToResponse(this SearchHitDto dto) =>
        new(dto.ObjectType, dto.ObjectId, dto.OwnerId, dto.Title, dto.Snippet,
            dto.Tags, dto.IsPublic, dto.UpdatedAt, dto.Rank);

    public static ExploreItemResponse ToResponse(this ExploreItemDto dto) =>
        new(dto.ObjectType, dto.ObjectId, dto.OwnerId, dto.Title, dto.Body,
            dto.Tags, dto.UpdatedAt, dto.Score);

    public static FeedItemResponse ToResponse(this FeedItemDto dto) =>
        new(dto.ObjectType, dto.ObjectId, dto.OwnerId, dto.Title, dto.Body,
            dto.Tags, dto.UpdatedAt, dto.Reason);

    public static CursorPagedResponse<TResponse> ToCursorResponse<TDto, TResponse>(
        this CursorPagedList<TDto> page,
        Func<TDto, TResponse> map) =>
        new(page.Items.Select(map).ToList(), page.NextCursor, page.NextCursor is not null);
}
