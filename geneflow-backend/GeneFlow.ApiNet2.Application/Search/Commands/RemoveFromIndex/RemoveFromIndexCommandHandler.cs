using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Commands.RemoveFromIndex;

public sealed class RemoveFromIndexCommandHandler : ICommandHandler<RemoveFromIndexCommand, Result>
{
    private readonly ISearchIndexRepository _repository;
    private readonly ISearchUnitOfWork _unitOfWork;

    public RemoveFromIndexCommandHandler(
        ISearchIndexRepository repository,
        ISearchUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveFromIndexCommand request, CancellationToken cancellationToken)
    {
        if (!SearchObjectType.TryFromName(request.ObjectType, out var objectType) || objectType is null)
            return Result.Failure(SearchErrors.InvalidObjectType);

        var existing = await _repository.GetAsync(objectType, request.ObjectId, cancellationToken);
        if (existing is null)
            return Result.Success();

        _repository.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
