using GeneFlow.ApiNet2.Application.Subscriptions.Queries.GetCurrentSubscription;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Application.Subscriptions.Queries;

/// <summary>
/// Unit tests for GetCurrentSubscriptionQueryHandler.
/// </summary>
public class GetCurrentSubscriptionQueryHandlerTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly GetCurrentSubscriptionQueryHandler _handler;

    public GetCurrentSubscriptionQueryHandlerTests()
    {
        _handler = new GetCurrentSubscriptionQueryHandler(_subscriptionRepository);
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
    public async Task Handle_WithActiveSubscription_ShouldReturnSubscriptionDto()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var query = new GetCurrentSubscriptionQuery("U00000001");

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlanName.Should().Be("Pro");
        result.Value.Status.Should().Be("Active");
        result.Value.GrantsAccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFreeSubscription_ShouldReturnFreeDto()
    {
        // Arrange
        var subscription = CreateFreeSubscription();
        var query = new GetCurrentSubscriptionQuery("U00000001");

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlanName.Should().Be("Free");
        result.Value.IsFree.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithTrialSubscription_ShouldReturnTrialDto()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);
        var query = new GetCurrentSubscriptionQuery("U00000001");

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Trial");
        result.Value.IsInTrial.Should().BeTrue();
        result.Value.TrialEndDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldIncludeCurrentPeriodInfo()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var query = new GetCurrentSubscriptionQuery("U00000001");

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentPeriod.Should().NotBeNull();
        result.Value.CurrentPeriod.StartDate.Should().BeOnOrBefore(DateTime.UtcNow);
        result.Value.CurrentPeriod.DaysRemaining.Should().BeGreaterThan(0);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetCurrentSubscriptionQuery("invalid-user-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Not Found Cases

    [Fact]
    public async Task Handle_WhenNoSubscription_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetCurrentSubscriptionQuery("U00000001");

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectUserId()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var query = new GetCurrentSubscriptionQuery("U00000001");

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _subscriptionRepository.Received(1).GetActiveByUserIdAsync(
            Arg.Is<UserId>(id => id.Value == 1),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
