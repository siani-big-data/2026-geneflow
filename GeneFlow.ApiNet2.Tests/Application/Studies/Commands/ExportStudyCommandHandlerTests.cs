using GeneFlow.ApiNet2.Application.Studies.Commands.ExportStudy;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.Domain.Traces;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

public class ExportStudyCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ExportStudyCommandHandler _handler;

    public ExportStudyCommandHandlerTests()
    {
        _handler = new ExportStudyCommandHandler(_studyRepository, _traceRepository);
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        var command = new ExportStudyCommand("S00000001", "not-a-user-id");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StudyErrors.InvalidUserId.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnNotFound()
    {
        var command = new ExportStudyCommand("not-a-study", "U00000001");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StudyErrors.NotFound.Code);
    }

    [Fact]
    public async Task Handle_WhenStudyDoesNotExist_ShouldReturnNotFound()
    {
        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);
        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        var command = new ExportStudyCommand("S99999999", "U00000001");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StudyErrors.NotFound.Code);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotMember_ShouldReturnNotAMember()
    {
        var ownerId = new UserId(1);
        var study = CreateStudy(ownerId);
        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Use a different user that is not in Members.
        var command = new ExportStudyCommand("S00000001", "U00000099");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StudyErrors.NotAMember.Code);
    }

    [Fact]
    public async Task Handle_WithMemberAndZeroTracesZeroPapers_ShouldReturnEmptyDto()
    {
        var ownerId = new UserId(1);
        var study = CreateStudy(ownerId);
        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);
        _traceRepository
            .GetByStudyIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Trace>)Array.Empty<Trace>());

        var command = new ExportStudyCommand("S00000001", "U00000001");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Traces.Should().BeEmpty();
        result.Value.Papers.Should().BeEmpty();
        result.Value.Members.Should().HaveCount(1); // owner is auto-added
        result.Value.Study.Title.Should().Be("Test Study");
        result.Value.ArchiveFileName.Should().StartWith("study-").And.EndWith(".zip");
    }

    [Fact]
    public async Task Handle_WhenMember_ShouldIncludeMetadataAndCounts()
    {
        var ownerId = new UserId(1);
        var study = CreateStudy(ownerId, title: "My Research Study");
        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);
        _traceRepository
            .GetByStudyIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Trace>)Array.Empty<Trace>());

        var command = new ExportStudyCommand("S00000001", "U00000001");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Study.Id.Should().Be("S00000001");
        result.Value.Study.ResearchField.Should().Be(ResearchField.Genomics.Name);
        result.Value.ArchiveFileName.Should().Contain("my-research-study");
    }

    [Fact]
    public void BuildArchiveFileName_LongTitle_ShouldBeCappedAt80Chars()
    {
        var longTitle = new string('a', 200);

        var name = ExportStudyCommandHandler.BuildArchiveFileName(longTitle, "S00000001");

        name.Length.Should().BeLessThanOrEqualTo(80);
        name.Should().EndWith("-S00000001.zip");
    }

    [Fact]
    public void BuildArchiveFileName_TitleWithUnicodeAndPunctuation_ShouldBeAsciiSlug()
    {
        var name = ExportStudyCommandHandler.BuildArchiveFileName("Étude / Genómica! (2024)", "S00000001");

        name.Should().StartWith("study-");
        name.Should().EndWith("-S00000001.zip");
        // No spaces, no slashes, no parens, no accents.
        name.Should().NotContainAny(" ", "/", "\\", "(", ")", "!", "É", "ó");
    }

    private static Study CreateStudy(UserId ownerId, string title = "Test Study")
    {
        var titleVo = StudyTitle.Create(title).Value;
        var description = StudyDescription.Create("Description").Value;
        return Study.Create(new StudyId(1), ownerId, titleVo, description, ResearchField.Genomics).Value;
    }
}
