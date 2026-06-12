using GeneFlow.ApiNet2.Application.Profiles.Commands.UpdatePinnedStudies;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using IDomainEventDispatcher = GeneFlow.ApiNet2.SharedKernel.Infrastructure.IDomainEventDispatcher;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Commands;

/// <summary>
/// Unit tests for UpdatePinnedStudiesCommandHandler.
/// </summary>
public class UpdatePinnedStudiesCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IProfileUnitOfWork _unitOfWork = Substitute.For<IProfileUnitOfWork>();
    private readonly IDomainEventDispatcher _eventDispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly UpdatePinnedStudiesCommandHandler _handler;

    public UpdatePinnedStudiesCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _profileRepository
            .ReplacePinnedStudiesAsync(
                Arg.Any<UserId>(),
                Arg.Any<IReadOnlyList<StudyId>>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StudyId>());

        _handler = new UpdatePinnedStudiesCommandHandler(
            _profileRepository, _unitOfWork, _eventDispatcher);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReplaceAndSave()
    {
        var command = new UpdatePinnedStudiesCommand(
            "U00000001",
            new[] { "S00000001", "S00000002" });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _profileRepository.Received(1).ReplacePinnedStudiesAsync(
            Arg.Any<UserId>(),
            Arg.Is<IReadOnlyList<StudyId>>(s => s.Count == 2),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidUserId_ShouldFail()
    {
        var command = new UpdatePinnedStudiesCommand("invalid", Array.Empty<string>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProfileErrors.InvalidUserId.Code);
    }

    [Fact]
    public async Task Handle_TooManyPinned_ShouldFail()
    {
        var seven = Enumerable.Range(1, 7).Select(i => $"S{i:D8}").ToArray();
        var command = new UpdatePinnedStudiesCommand("U00000001", seven);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProfileErrors.TooManyPinned.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateIds_ShouldFail()
    {
        var command = new UpdatePinnedStudiesCommand(
            "U00000001",
            new[] { "S00000001", "S00000001" });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProfileErrors.DuplicatePinned.Code);
    }

    [Fact]
    public async Task Handle_InvalidStudyId_ShouldFail()
    {
        var command = new UpdatePinnedStudiesCommand(
            "U00000001",
            new[] { "not-a-study-id" });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProfileErrors.InvalidStudyId.Code);
    }

    [Fact]
    public async Task Handle_EmptyList_ShouldSucceedAndClear()
    {
        var command = new UpdatePinnedStudiesCommand("U00000001", Array.Empty<string>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _profileRepository.Received(1).ReplacePinnedStudiesAsync(
            Arg.Any<UserId>(),
            Arg.Is<IReadOnlyList<StudyId>>(s => s.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewPin_EmitsStudyPinnedEvent()
    {
        var command = new UpdatePinnedStudiesCommand("U00000001", new[] { "S00000001" });

        await _handler.Handle(command, CancellationToken.None);

        await _eventDispatcher.Received(1).DispatchAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }
}
