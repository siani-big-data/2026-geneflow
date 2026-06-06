using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Entities;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Commands.IndexObject;

public sealed class IndexObjectCommandHandler : ICommandHandler<IndexObjectCommand, Result>
{
    private readonly ISearchIndexRepository _repository;
    private readonly ISearchUnitOfWork _unitOfWork;

    public IndexObjectCommandHandler(
        ISearchIndexRepository repository,
        ISearchUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(IndexObjectCommand request, CancellationToken cancellationToken)
    {
        if (!SearchObjectType.TryFromName(request.ObjectType, out var objectType) || objectType is null)
            return Result.Failure(SearchErrors.InvalidObjectType);

        var existing = await _repository.GetAsync(objectType, request.ObjectId, cancellationToken);
        if (existing is not null)
        {
            existing.Update(request.Title, request.Body, request.Tags, request.IsPublic, request.OwnerId);
            _repository.Update(existing);
        }
        else
        {
            var created = SearchIndexEntry.Create(
                objectType,
                request.ObjectId,
                request.OwnerId,
                request.Title,
                request.Body,
                request.Tags,
                request.IsPublic);

            if (created.IsFailure)
                return Result.Failure(created.Error);

            await _repository.AddAsync(created.Value, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
