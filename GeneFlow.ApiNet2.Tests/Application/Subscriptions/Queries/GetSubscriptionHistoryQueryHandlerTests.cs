using GeneFlow.ApiNet2.Application.Subscriptions.Queries.GetSubscriptionHistory;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Application.Subscriptions.Queries;

/// <summary>
/// Unit tests for GetSubscriptionHistoryQueryHandler.
/// </summary>
public class GetSubscriptionHistoryQueryHandlerTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly GetSubscriptionHistoryQueryHandler _handler;

    public GetSubscriptionHistoryQueryHandlerTests()
    {
        _handler = new GetSubscriptionHistoryQueryHandler(_subscriptionRepository);
    }

    #region Helper Methods

    private static long _subscriptionSequence = 1;
    private static long _planSequence = 1;

    private static Subscription CreateTestSubscription(
        long userId = 1,
        string planName = "Pro",
        bool startWithTrial = false)
    {
        return Subscription.Create(
            SubscriptionId.FromSequence(_subscriptionSequence++),
            new UserId(userId),
            PlanId.FromSequence(_planSequence++),
            planName,
            BillingCycle.Monthly,
            startWithTrial).Value;
    }

    private static Subscription CreateFreeSubscription(long userId = 1)
    {
        return Subscription.CreateFree(
            SubscriptionId.FromSequence(_subscriptionSequence++),
            new UserId(userId),
            PlanId.FromSequence(_planSequence++)).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithSubscriptionHistory_ShouldReturnSubscriptionList()
    {
        // Arrange
        var subscription1 = CreateTestSubscription(planName: "Pro");
        var subscription2 = CreateTestSubscription(planName: "Enterprise");
        var subscriptions = new List<Subscription> { subscription1, subscription2 };
        var query = new GetSubscriptionHistoryQuery("U00000001");

        _subscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].PlanName.Should().Be("Pro");
        result.Value[1].PlanName.Should().Be("Enterprise");
    }

    [Fact]
    public async Task Handle_WithEmptyHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetSubscriptionHistoryQuery("U00000001");

        _subscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Subscription>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnSummaryDtos()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Pro");
        var subscriptions = new List<Subscription> { subscription };
        var query = new GetSubscriptionHistoryQuery("U00000001");

        _subscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var summary = result.Value.First();
        summary.PlanName.Should().Be("Pro");
        summary.Status.Should().Be("Active");
        summary.GrantsAccess.Should().BeTrue();
        summary.StartDate.Should().BeOnOrBefore(DateTime.UtcNow);
        summary.EndDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_WithMixedSubscriptions_ShouldReturnAllStatuses()
    {
        // Arrange
        var activeSubscription = CreateTestSubscription(planName: "Pro");
        var cancelledSubscription = CreateTestSubscription(planName: "Basic");
        cancelledSubscription.Cancel("Test cancellation");

        var subscriptions = new List<Subscription> { activeSubscription, cancelledSubscription };
        var query = new GetSubscriptionHistoryQuery("U00000001");

        _subscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(s => s.Status == "Active");
        result.Value.Should().Contain(s => s.Status == "Cancelled");
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public async Task Handle_ShouldReturnSubscriptionsInRepositoryOrder()
    {
        // Arrange
        // Note: The handler returns subscriptions in the order provided by the repository
        // The repository is responsible for ordering (typically by date descending)
        var sub1 = CreateTestSubscription(planName: "Basic");
        var sub2 = CreateTestSubscription(planName: "Pro");
        var sub3 = CreateTestSubscription(planName: "Enterprise");

        var subscriptions = new List<Subscription> { sub1, sub2, sub3 };
        var query = new GetSubscriptionHistoryQuery("U00000001");

        _subscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        // Verify the order is preserved from repository
        result.Value[0].PlanName.Should().Be("Basic");
        result.Value[1].PlanName.Should().Be("Pro");
        result.Value[2].PlanName.Should().Be("Enterprise");
    }

    [Fact]
    public async Task Handle_WithMultipleStatuses_ShouldPreserveOrderFromRepository()
    {
        // Arrange
        var activeSub = CreateTestSubscription(planName: "Pro");
        var cancelledSub = CreateTestSubscription(planName: "Basic");
        cancelledSub.Cancel("Test");
        var expiredSub = CreateTestSubscription(planName: "Starter");
        expiredSub.Expire();

        // Repository returns in specific order (simulating date descending)
        var subscriptions = new List<Subscription> { activeSub, cancelledSub, expiredSub };
        var query = new GetSubscriptionHistoryQuery("U00000001");

        _subscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].Status.Should().Be("Active");
        result.Value[1].Status.Should().Be("Cancelled");
        result.Value[2].Status.Should().Be("Expired");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetSubscriptionHistoryQuery("invalid-user-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetSubscriptionHistoryQuery("");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectUserId()
    {
        // Arrange
        var query = new GetSubscriptionHistoryQuery("U00000001");

        _subscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Subscription>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _subscriptionRepository.Received(1).GetAllByUserIdAsync(
            Arg.Is<UserId>(id => id.Value == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var query = new GetSubscriptionHistoryQuery("U00000001");
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _subscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Subscription>());

        // Act
        await _handler.Handle(query, token);

        // Assert
        await _subscriptionRepository.Received(1).GetAllByUserIdAsync(
            Arg.Any<UserId>(),
            token);
    }

    #endregion
}
