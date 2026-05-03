using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Common.Base;

/// <summary>
/// Base class for handler tests providing common mocks and setup.
/// </summary>
public abstract class HandlerTestBase
{
    protected readonly IUserRepository UserRepository;
    protected readonly IUserUnitOfWork UserUnitOfWork;
    protected readonly IStudyRepository StudyRepository;
    protected readonly IStudyUnitOfWork StudyUnitOfWork;
    protected readonly ITraceRepository TraceRepository;
    protected readonly ITraceUnitOfWork TraceUnitOfWork;
    protected readonly IPipelineRepository PipelineRepository;
    protected readonly IPipelineUnitOfWork PipelineUnitOfWork;
    protected readonly IPipelineExecutionRepository PipelineExecutionRepository;
    protected readonly ISubscriptionRepository SubscriptionRepository;
    protected readonly ISubscriptionUnitOfWork SubscriptionUnitOfWork;
    protected readonly ISequenceGenerator SequenceGenerator;

    protected HandlerTestBase()
    {
        UserRepository = Substitute.For<IUserRepository>();
        UserUnitOfWork = Substitute.For<IUserUnitOfWork>();
        StudyRepository = Substitute.For<IStudyRepository>();
        StudyUnitOfWork = Substitute.For<IStudyUnitOfWork>();
        TraceRepository = Substitute.For<ITraceRepository>();
        TraceUnitOfWork = Substitute.For<ITraceUnitOfWork>();
        PipelineRepository = Substitute.For<IPipelineRepository>();
        PipelineUnitOfWork = Substitute.For<IPipelineUnitOfWork>();
        PipelineExecutionRepository = Substitute.For<IPipelineExecutionRepository>();
        SubscriptionRepository = Substitute.For<ISubscriptionRepository>();
        SubscriptionUnitOfWork = Substitute.For<ISubscriptionUnitOfWork>();
        SequenceGenerator = Substitute.For<ISequenceGenerator>();

        // Setup default sequence generator behavior
        SequenceGenerator.NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Random.Shared.NextInt64(1, 1_000_000)));
    }

    protected static UserId CreateUserId(long value = 1) => new(value);
    protected static StudyId CreateStudyId(long value = 1) => new(value);
    protected static TraceId CreateTraceId() => TraceId.New();
    protected static PipelineId CreatePipelineId(long value = 1) => new(value);
    protected static PipelineExecutionId CreateExecutionId(long value = 1) => new(value);

    protected void SetupSequenceGenerator(long nextValue)
    {
        SequenceGenerator.NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(nextValue));
    }
}
