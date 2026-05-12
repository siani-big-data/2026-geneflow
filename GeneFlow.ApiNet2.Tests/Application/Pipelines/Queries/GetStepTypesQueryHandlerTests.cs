using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStepTypes;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Queries;

/// <summary>
/// Unit tests for GetStepTypesQueryHandler.
/// </summary>
public class GetStepTypesQueryHandlerTests
{
    private readonly GetStepTypesQueryHandler _handler;

    public GetStepTypesQueryHandlerTests()
    {
        _handler = new GetStepTypesQueryHandler();
    }

    #region Success Cases

    [Fact]
    public async Task Handle_ShouldReturnAllStepTypes()
    {
        // Arrange
        var query = new GetStepTypesQuery("U00000001");
        var expectedStepTypes = StepType.GetAll().ToList();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(expectedStepTypes.Count);
    }

    [Fact]
    public async Task Handle_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var query = new GetStepTypesQuery("U00000001");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify Quality step type properties
        var qualityDto = result.Value.FirstOrDefault(s => s.Name == StepType.Quality.Name);
        qualityDto.Should().NotBeNull();
        qualityDto!.Id.Should().Be(StepType.Quality.Id);
        qualityDto.Name.Should().Be(StepType.Quality.Name);
        qualityDto.DisplayName.Should().Be(StepType.Quality.DisplayName);
        qualityDto.AnalysisKey.Should().Be(StepType.Quality.AnalysisKey);
        qualityDto.RequiresConfiguration.Should().Be(StepType.Quality.RequiresConfiguration);
        qualityDto.ConfigurationSchema.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldIncludeAllKnownStepTypes()
    {
        // Arrange
        var query = new GetStepTypesQuery("U00000001");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var stepTypeNames = result.Value.Select(s => s.Name).ToList();
        stepTypeNames.Should().Contain(StepType.Quality.Name);
        stepTypeNames.Should().Contain(StepType.Trimming.Name);
        stepTypeNames.Should().Contain(StepType.Heterozygote.Name);
        stepTypeNames.Should().Contain(StepType.Motif.Name);
        stepTypeNames.Should().Contain(StepType.Translation.Name);
        stepTypeNames.Should().Contain(StepType.ORF.Name);
        stepTypeNames.Should().Contain(StepType.Restriction.Name);
    }

    #endregion
}
