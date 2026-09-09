using GeneFlow.ApiNet2.Application.Usage.Queries.GetBillingUsage;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using GeneFlow.ApiNet2.Domain.Usage;

namespace GeneFlow.ApiNet2.Tests.Application.Usage.Queries;

/// <summary>
/// Unit tests for GetBillingUsageQueryHandler.
/// </summary>
public class GetBillingUsageQueryHandlerTests
{
    private readonly IUsageStatsRepository _usageRepository = Substitute.For<IUsageStatsRepository>();
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly GetBillingUsageQueryHandler _handler;

    public GetBillingUsageQueryHandlerTests()
    {
        _handler = new GetBillingUsageQueryHandler(
            _usageRepository,
            _subscriptionRepository,
            _planRepository);
    }

    #region Helper Methods

    private static long _planSequence = 1;
    private static long _subscriptionSequence = 1;

    private static Plan CreateTestPlan(int maxStudies = 10, int maxTraces = 500, int maxMembers = 10)
    {
        var planId = PlanId.FromSequence(_planSequence++);
        var planName = PlanName.Create("Pro").Value;
        var pricing = PlanPricing.Create(29, 290, "EUR").Value;
        var limits = PlanLimits.Create(maxStudies, maxTraces, maxMembers).Value;

        return Plan.Create(planId, planName, "Pro plan", pricing, limits).Value;
    }

    private static Subscription CreateTestSubscription(UserId userId, PlanId planId)
    {
        var subscriptionId = SubscriptionId.FromSequence(_subscriptionSequence++);
        return Subscription.Create(subscriptionId, userId, planId, "Pro", BillingCycle.Monthly).Value;
    }

    private static UsageStats CreateTestUsageStats(UserId userId, int studiesOwned = 3, int traces = 100, int maxMembers = 5)
    {
        var stats = UsageStats.Create(userId, BillingPeriodKey.Current());
        stats.SetStats(
            studiesOwned: studiesOwned,
            studiesTotal: studiesOwned + 2,
            tracesThisPeriod: traces,
            tracesTotal: traces * 10,
            maxMembersInStudy: maxMembers,
            alignmentsThisPeriod: 10,
            alignmentsTotal: 100,
            alignmentsCompleted: 5,
            tracesPending: 2,
            lastActivityAt: DateTime.UtcNow);
        return stats;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidUserId_ShouldReturnBillingUsage()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var plan = CreateTestPlan();
        var subscription = CreateTestSubscription(userId, plan.Id);
        var stats = CreateTestUsageStats(userId);
        var query = new GetBillingUsageQuery(userId.ToString());

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _usageRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(stats);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Studies.Used.Should().Be(3);
        result.Value.Studies.Total.Should().Be(10);
        result.Value.Traces.Used.Should().Be(100);
        result.Value.Traces.Total.Should().Be(500);
        result.Value.Members.Used.Should().Be(5);
        result.Value.Members.Total.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldCalculatePercentagesCorrectly()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var plan = CreateTestPlan(maxStudies: 10, maxTraces: 100, maxMembers: 10);
        var subscription = CreateTestSubscription(userId, plan.Id);
        var stats = CreateTestUsageStats(userId, studiesOwned: 5, traces: 50, maxMembers: 5);
        var query = new GetBillingUsageQuery(userId.ToString());

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _usageRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(stats);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Studies.Percentage.Should().Be(50);
        result.Value.Traces.Percentage.Should().Be(50);
        result.Value.Members.Percentage.Should().Be(50);
    }

    [Fact]
    public async Task Handle_WhenNoUsageStats_ShouldReturnZeroUsage()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var plan = CreateTestPlan();
        var subscription = CreateTestSubscription(userId, plan.Id);
        var query = new GetBillingUsageQuery(userId.ToString());

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _usageRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UsageStats?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Studies.Used.Should().Be(0);
        result.Value.Traces.Used.Should().Be(0);
        result.Value.Members.Used.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnBillingPeriodInfo()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var plan = CreateTestPlan();
        var subscription = CreateTestSubscription(userId, plan.Id);
        var query = new GetBillingUsageQuery(userId.ToString());

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _usageRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UsageStats?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Period.Should().NotBeNull();
        result.Value.Period.StartDate.Should().BeBefore(DateTime.UtcNow);
        result.Value.Period.EndDate.Should().BeAfter(DateTime.UtcNow);
        result.Value.Period.TotalDays.Should().BeGreaterThan(0);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetBillingUsageQuery("invalid-user-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNoActiveSubscription_ShouldReturnFailure()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var query = new GetBillingUsageQuery(userId.ToString());

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenPlanNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var plan = CreateTestPlan();
        var subscription = CreateTestSubscription(userId, plan.Id);
        var query = new GetBillingUsageQuery(userId.ToString());

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns((Plan?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallAllRepositories()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var plan = CreateTestPlan();
        var subscription = CreateTestSubscription(userId, plan.Id);
        var query = new GetBillingUsageQuery(userId.ToString());

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _usageRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UsageStats?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _subscriptionRepository.Received(1).GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
        await _planRepository.Received(1).GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>());
        await _usageRepository.Received(1).GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
