using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Common.Builders;

/// <summary>
/// Builder pattern for creating Pipeline test data.
/// </summary>
public class PipelineBuilder
{
    private long _id = 1;
    private long _studyId = 1;
    private long _ownerId = 1;
    private string _name = "Test Pipeline";
    private string? _description = "Test pipeline description";
    private PipelineStatus _status = PipelineStatus.Draft;
    private readonly List<(StepType Type, string? Label, string Config, bool IsEnabled)> _steps = new();

    public PipelineBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public PipelineBuilder WithStudyId(long studyId)
    {
        _studyId = studyId;
        return this;
    }

    public PipelineBuilder WithOwnerId(long ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public PipelineBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PipelineBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public PipelineBuilder WithStatus(PipelineStatus status)
    {
        _status = status;
        return this;
    }

    public PipelineBuilder WithStep(StepType type, string? label = null, string config = "{}", bool isEnabled = true)
    {
        _steps.Add((type, label, config, isEnabled));
        return this;
    }

    public PipelineBuilder WithSteps(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _steps.Add((StepType.Quality, $"Step {i + 1}", "{}", true));
        }
        return this;
    }

    public PipelineBuilder WithQualityStep(string? label = "Quality Analysis")
    {
        return WithStep(StepType.Quality, label);
    }

    public PipelineBuilder WithTrimmingStep(double cutoff = 0.1, string? label = "Trimming")
    {
        return WithStep(StepType.Trimming, label, $$$"""{"cutoff": {{{cutoff}}}}""");
    }

    public PipelineBuilder WithMotifStep(string pattern, string? label = "Motif Search")
    {
        return WithStep(StepType.Motif, label, $$$"""{"pattern": "{{{pattern}}}"}""");
    }

    public PipelineBuilder WithRestrictionStep(string[] enzymes, string? label = "Restriction Sites")
    {
        var enzymeList = string.Join(", ", enzymes.Select(e => $"\"{e}\""));
        return WithStep(StepType.Restriction, label, $$$"""{"enzymes": [{{{enzymeList}}}]}""");
    }

    public PipelineBuilder AsActive()
    {
        _status = PipelineStatus.Active;
        if (_steps.Count == 0)
        {
            WithQualityStep();
        }
        return this;
    }

    public PipelineBuilder AsArchived()
    {
        _status = PipelineStatus.Archived;
        return this;
    }

    public Pipeline Build()
    {
        var pipelineId = new PipelineId(_id);
        var studyId = new StudyId(_studyId);
        var ownerId = new UserId(_ownerId);
        var name = PipelineName.Create(_name).Value;
        var description = PipelineDescription.Create(_description).Value;

        var pipeline = Pipeline.Create(pipelineId, studyId, ownerId, name, description).Value;

        // Add steps
        foreach (var (type, label, config, isEnabled) in _steps)
        {
            var stepConfig = StepConfiguration.Create(config, type).Value;
            pipeline.AddStep(type, stepConfig, label, isEnabled, ownerId);
        }

        // Handle status transitions
        if (_status == PipelineStatus.Active)
        {
            if (pipeline.EnabledStepCount == 0)
            {
                // Add a default step if none exist
                var defaultConfig = StepConfiguration.Create("{}", StepType.Quality).Value;
                pipeline.AddStep(StepType.Quality, defaultConfig, "Default Step", true, ownerId);
            }
            pipeline.Activate(ownerId);
        }
        else if (_status == PipelineStatus.Archived)
        {
            pipeline.Archive(ownerId);
        }

        pipeline.ClearDomainEvents();
        return pipeline;
    }

    /// <summary>
    /// Creates a new builder with default values.
    /// </summary>
    public static PipelineBuilder Default() => new();

    /// <summary>
    /// Creates a builder for a draft pipeline with steps.
    /// </summary>
    public static PipelineBuilder DraftWithSteps(int stepCount = 1) =>
        new PipelineBuilder().WithSteps(stepCount);

    /// <summary>
    /// Creates a builder for an active pipeline.
    /// </summary>
    public static PipelineBuilder Active() =>
        new PipelineBuilder().AsActive();

    /// <summary>
    /// Creates a builder for an archived pipeline.
    /// </summary>
    public static PipelineBuilder Archived() =>
        new PipelineBuilder().AsArchived();
}
