using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Activity.Enumerations;

/// <summary>
/// Type of the object the actor acted upon.
/// Aligned with the bounded contexts that publish events on the bus.
/// </summary>
public sealed class ActivityObjectType : Enumeration<ActivityObjectType>
{
    public static readonly ActivityObjectType User = new(1, nameof(User));
    public static readonly ActivityObjectType Profile = new(2, nameof(Profile));
    public static readonly ActivityObjectType Study = new(3, nameof(Study));
    public static readonly ActivityObjectType Trace = new(4, nameof(Trace));
    public static readonly ActivityObjectType Alignment = new(5, nameof(Alignment));
    public static readonly ActivityObjectType Analysis = new(6, nameof(Analysis));
    public static readonly ActivityObjectType Pipeline = new(7, nameof(Pipeline));
    public static readonly ActivityObjectType PipelineExecution = new(8, nameof(PipelineExecution));
    public static readonly ActivityObjectType Subscription = new(9, nameof(Subscription));
    public static readonly ActivityObjectType Plan = new(10, nameof(Plan));
    public static readonly ActivityObjectType Member = new(11, nameof(Member));
    public static readonly ActivityObjectType Comment = new(12, nameof(Comment));
    public static readonly ActivityObjectType Annotation = new(13, nameof(Annotation));
    public static readonly ActivityObjectType SequenceEdit = new(14, nameof(SequenceEdit));
    public static readonly ActivityObjectType TraceTrim = new(15, nameof(TraceTrim));
    public static readonly ActivityObjectType Other = new(99, nameof(Other));

    private ActivityObjectType(int id, string name) : base(id, name) { }
}
