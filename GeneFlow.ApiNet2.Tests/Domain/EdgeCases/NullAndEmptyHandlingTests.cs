using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Enumerations;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.EdgeCases;

/// <summary>
/// Comprehensive tests for null and empty handling across all domain entities and value objects.
/// </summary>
public class NullAndEmptyHandlingTests
{
    #region Value Object Null Handling

    [Fact]
    public void Email_WithNull_ShouldReturnError()
    {
        // Act
        var result = Email.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Email");
    }

    [Fact]
    public void Email_WithEmpty_ShouldReturnError()
    {
        // Act
        var result = Email.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Email");
    }

    [Fact]
    public void Username_WithNull_ShouldReturnError()
    {
        // Act
        var result = Username.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Username");
    }

    [Fact]
    public void Username_WithEmpty_ShouldReturnError()
    {
        // Act
        var result = Username.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Username");
    }

    [Fact]
    public void StudyTitle_WithNull_ShouldReturnError()
    {
        // Act
        var result = StudyTitle.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Title");
    }

    [Fact]
    public void StudyTitle_WithEmpty_ShouldReturnError()
    {
        // Act
        var result = StudyTitle.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Title");
    }

    [Fact]
    public void StudyDescription_WithNull_ShouldSucceed()
    {
        // Act
        var result = StudyDescription.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void StudyDescription_WithEmpty_ShouldSucceed()
    {
        // Act
        var result = StudyDescription.Create(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void PipelineName_WithNull_ShouldReturnError()
    {
        // Act
        var result = PipelineName.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Fact]
    public void PipelineName_WithEmpty_ShouldReturnError()
    {
        // Act
        var result = PipelineName.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Fact]
    public void PipelineDescription_WithNull_ShouldSucceed()
    {
        // Act
        var result = PipelineDescription.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void PipelineDescription_WithEmpty_ShouldSucceed()
    {
        // Act
        var result = PipelineDescription.Create(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void TraceName_WithNull_ShouldReturnError()
    {
        // Act
        var result = TraceName.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Fact]
    public void TraceName_WithEmpty_ShouldReturnError()
    {
        // Act
        var result = TraceName.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Fact]
    public void TraceDescription_WithNull_ShouldSucceed()
    {
        // Act
        var result = TraceDescription.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void TraceDescription_WithEmpty_ShouldSucceed()
    {
        // Act
        var result = TraceDescription.Create(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Bio_WithNull_ShouldSucceed()
    {
        // Act
        var result = Bio.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Bio_WithEmpty_ShouldSucceed()
    {
        // Act
        var result = Bio.Create(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Location_WithNull_ShouldSucceed()
    {
        // Act
        var result = Location.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Location_WithEmpty_ShouldSucceed()
    {
        // Act
        var result = Location.Create(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Institution_WithNull_ShouldSucceed()
    {
        // Act
        var result = Institution.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().BeNull();
        result.Value.Department.Should().BeNull();
    }

    [Fact]
    public void Institution_WithEmpty_ShouldSucceed()
    {
        // Act
        var result = Institution.Create(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().BeNull();
    }

    #endregion

    #region Entity Optional Field Handling

    [Fact]
    public void Study_CreateWithNullDescription_ShouldSucceed()
    {
        // Arrange
        var studyId = new StudyId(1);
        var ownerId = new UserId(1);
        var titleResult = StudyTitle.Create("Valid Study Title");
        var descriptionResult = StudyDescription.Create(null);
        var researchField = GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField.Genomics;

        // Act
        var result = Study.Create(
            studyId,
            ownerId,
            titleResult.Value,
            descriptionResult.Value,
            researchField);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Value.Should().BeNull();
    }

    [Fact]
    public void Study_UpdateWithNullInstitution_ShouldClearField()
    {
        // Arrange
        var study = CreateValidStudy();
        var ownerId = study.OwnerId;

        // First set an institution
        study.UpdateInstitution("Initial Institution", ownerId);
        study.Institution.Should().Be("Initial Institution");

        // Act - clear by setting null
        var result = study.UpdateInstitution(null, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.Institution.Should().BeNull();
    }

    [Fact]
    public void Profile_CreateWithNullBio_ShouldSucceed()
    {
        // Arrange
        var profileId = new ProfileId(1);
        var userId = new UserId(1);
        var nameResult = PersonName.Create("John", "Doe");

        // Act
        var result = Profile.Create(profileId, userId, nameResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Bio.Value.Should().BeNull();
    }

    [Fact]
    public void Profile_UpdateWithNullLocation_ShouldClearField()
    {
        // Arrange
        var profile = CreateValidProfile();
        var locationResult = Location.Create(null);
        var bioResult = Bio.Create(null);
        var professionalRoleResult = ProfessionalRole.Create(null);
        var institutionResult = Institution.Create(null);

        // Act
        var result = profile.UpdateBasicInfo(
            profile.Name,
            bioResult.Value,
            locationResult.Value,
            professionalRoleResult.Value,
            institutionResult.Value,
            null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Location.Value.Should().BeNull();
    }

    [Fact]
    public void Trace_CreateWithNullDescription_ShouldSucceed()
    {
        // Arrange
        var traceId = TraceId.New();
        var studyId = new StudyId(1);
        var uploadedBy = new UserId(1);
        var nameResult = TraceName.Create("Valid Trace Name");
        var descriptionResult = TraceDescription.Create(null);
        var fileResult = TraceFile.Create("test.ab1", "application/octet-stream", "/storage/test.ab1", 1024, "abc123");
        var format = TraceFormat.AB1;

        // Act
        var result = Trace.Create(
            traceId,
            studyId,
            uploadedBy,
            nameResult.Value,
            descriptionResult.Value,
            fileResult.Value,
            format);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Value.Should().BeNull();
    }

    [Fact]
    public void Pipeline_CreateWithNullDescription_ShouldSucceed()
    {
        // Arrange
        var pipelineId = new PipelineId(1);
        var studyId = new StudyId(1);
        var ownerId = new UserId(1);
        var nameResult = PipelineName.Create("Valid Pipeline Name");
        var descriptionResult = PipelineDescription.Create(null);

        // Act
        var result = Pipeline.Create(
            pipelineId,
            studyId,
            ownerId,
            nameResult.Value,
            descriptionResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Value.Should().BeNull();
    }

    #endregion

    #region Collection Empty State

    [Fact]
    public void Study_Tags_WhenEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var study = CreateValidStudy();

        // Assert
        study.Tags.Should().NotBeNull();
        study.Tags.Should().BeEmpty();
        study.TagCount.Should().Be(0);
    }

    [Fact]
    public void Study_Papers_WhenEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var study = CreateValidStudy();

        // Assert
        study.Papers.Should().NotBeNull();
        study.Papers.Should().BeEmpty();
        study.PaperCount.Should().Be(0);
    }

    [Fact]
    public void Study_Members_ShouldAlwaysHaveOwner()
    {
        // Arrange
        var study = CreateValidStudy();

        // Assert
        study.Members.Should().NotBeNull();
        study.Members.Should().NotBeEmpty();
        study.Members.Should().HaveCount(1);
        study.Members.First().Role.Should().Be(StudyRole.Owner);
        study.Members.First().UserId.Should().Be(study.OwnerId);
    }

    [Fact]
    public void Pipeline_Steps_WhenEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var pipeline = CreateValidPipeline();

        // Assert
        pipeline.Steps.Should().NotBeNull();
        pipeline.Steps.Should().BeEmpty();
        pipeline.EnabledStepCount.Should().Be(0);
    }

    [Fact]
    public void Trace_Annotations_WhenEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var trace = CreateValidTrace();

        // Assert
        trace.Annotations.Should().NotBeNull();
        trace.Annotations.Should().BeEmpty();
        trace.AnnotationCount.Should().Be(0);
    }

    [Fact]
    public void Trace_Edits_WhenEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var trace = CreateValidTrace();

        // Assert
        trace.Edits.Should().NotBeNull();
        trace.Edits.Should().BeEmpty();
        trace.ActiveEditCount.Should().Be(0);
    }

    [Fact]
    public void Trace_Trims_WhenEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var trace = CreateValidTrace();

        // Assert
        trace.Trims.Should().NotBeNull();
        trace.Trims.Should().BeEmpty();
        trace.ActiveTrimCount.Should().Be(0);
    }

    #endregion

    #region Whitespace Handling

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t\n ")]
    public void Email_WithWhitespaceOnly_ShouldReturnError(string whitespace)
    {
        // Act
        var result = Email.Create(whitespace);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Email");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t\n ")]
    public void Username_WithWhitespaceOnly_ShouldReturnError(string whitespace)
    {
        // Act
        var result = Username.Create(whitespace);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Username");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t\n ")]
    public void StudyTitle_WithWhitespaceOnly_ShouldReturnError(string whitespace)
    {
        // Act
        var result = StudyTitle.Create(whitespace);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Title");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t\n ")]
    public void PipelineName_WithWhitespaceOnly_ShouldReturnError(string whitespace)
    {
        // Act
        var result = PipelineName.Create(whitespace);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t\n ")]
    public void TraceName_WithWhitespaceOnly_ShouldReturnError(string whitespace)
    {
        // Act
        var result = TraceName.Create(whitespace);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t\n ")]
    public void FailProcessing_WithWhitespaceOnlyReason_ShouldReturnError(string whitespace)
    {
        // Arrange
        var trace = CreateValidTraceForProcessing();

        // Start processing to enable FailProcessing transition
        trace.StartProcessing();

        // Act
        var result = trace.FailProcessing(whitespace);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FailureReason");
    }

    #endregion

    #region Optional Value Objects - Edge Cases

    [Fact]
    public void StudyDescription_Empty_Property_ShouldReturnNullValue()
    {
        // Act
        var empty = StudyDescription.Empty;

        // Assert
        empty.Value.Should().BeNull();
        empty.ToString().Should().BeEmpty();
    }

    [Fact]
    public void PipelineDescription_Empty_Property_ShouldReturnNullValue()
    {
        // Act
        var empty = PipelineDescription.Empty;

        // Assert
        empty.Value.Should().BeNull();
        empty.ToString().Should().BeEmpty();
    }

    [Fact]
    public void TraceDescription_Empty_Property_ShouldReturnNullValue()
    {
        // Act
        var empty = TraceDescription.Empty;

        // Assert
        empty.Value.Should().BeNull();
        empty.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Bio_Empty_Property_ShouldReturnNullValue()
    {
        // Act
        var empty = Bio.Empty;

        // Assert
        empty.Value.Should().BeNull();
        empty.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Location_Empty_Property_ShouldReturnNullValue()
    {
        // Act
        var empty = Location.Empty;

        // Assert
        empty.Value.Should().BeNull();
        empty.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Institution_Empty_Property_ShouldReturnNullValues()
    {
        // Act
        var empty = Institution.Empty;

        // Assert
        empty.Name.Should().BeNull();
        empty.Department.Should().BeNull();
        empty.DisplayName.Should().BeNull();
        empty.ToString().Should().BeEmpty();
    }

    #endregion

    #region Implicit Conversions with Null Values

    [Fact]
    public void StudyDescription_ImplicitConversion_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var description = StudyDescription.Create(null).Value;

        // Act
        string? result = description;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void PipelineDescription_ImplicitConversion_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var description = PipelineDescription.Create(null).Value;

        // Act
        string? result = description;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TraceDescription_ImplicitConversion_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var description = TraceDescription.Create(null).Value;

        // Act
        string? result = description;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Bio_ImplicitConversion_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var bio = Bio.Create(null).Value;

        // Act
        string? result = bio;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Location_ImplicitConversion_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var location = Location.Create(null).Value;

        // Act
        string? result = location;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static Study CreateValidStudy()
    {
        var studyId = new StudyId(1);
        var ownerId = new UserId(1);
        var titleResult = StudyTitle.Create("Valid Study Title");
        var descriptionResult = StudyDescription.Create("A valid study description.");
        var researchField = GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField.Genomics;

        return Study.Create(studyId, ownerId, titleResult.Value, descriptionResult.Value, researchField).Value;
    }

    private static Profile CreateValidProfile()
    {
        var profileId = new ProfileId(1);
        var userId = new UserId(1);
        var nameResult = PersonName.Create("John", "Doe");

        return Profile.Create(profileId, userId, nameResult.Value).Value;
    }

    private static Pipeline CreateValidPipeline()
    {
        var pipelineId = new PipelineId(1);
        var studyId = new StudyId(1);
        var ownerId = new UserId(1);
        var nameResult = PipelineName.Create("Valid Pipeline Name");
        var descriptionResult = PipelineDescription.Create("A valid pipeline description.");

        return Pipeline.Create(pipelineId, studyId, ownerId, nameResult.Value, descriptionResult.Value).Value;
    }

    private static Trace CreateValidTrace()
    {
        var traceId = TraceId.New();
        var studyId = new StudyId(1);
        var uploadedBy = new UserId(1);
        var nameResult = TraceName.Create("Valid Trace Name");
        var descriptionResult = TraceDescription.Create("A valid trace description.");
        var fileResult = TraceFile.Create("test.ab1", "application/octet-stream", "/storage/test.ab1", 1024, "abc123");
        var format = TraceFormat.AB1;

        return Trace.Create(traceId, studyId, uploadedBy, nameResult.Value, descriptionResult.Value, fileResult.Value, format).Value;
    }

    private static Trace CreateValidTraceForProcessing()
    {
        var traceId = TraceId.New();
        var studyId = new StudyId(1);
        var uploadedBy = new UserId(1);
        var nameResult = TraceName.Create("Trace For Processing Test");
        var descriptionResult = TraceDescription.Create("A trace for testing processing.");
        var fileResult = TraceFile.Create("processing-test.ab1", "application/octet-stream", "/storage/processing-test.ab1", 2048, "def456");
        var format = TraceFormat.AB1;

        return Trace.Create(traceId, studyId, uploadedBy, nameResult.Value, descriptionResult.Value, fileResult.Value, format).Value;
    }

    #endregion
}
