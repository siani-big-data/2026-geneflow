using GeneFlow.ApiNet2.Application.Subscriptions.Commands.CancelSubscription;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Application.Subscriptions.Commands;

/// <summary>
/// Unit tests for CancelSubscriptionCommandHandler.
/// </summary>
public class CancelSubscriptionCommandHandlerTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly ISubscriptionUnitOfWork _unitOfWork = Substitute.For<ISubscriptionUnitOfWork>();
    private readonly CancelSubscriptionCommandHandler _handler;

    public CancelSubscriptionCommandHandlerTests()
    {
        _handler = new CancelSubscriptionCommandHandler(
            _subscriptionRepository,
            _unitOfWork);
    }

    #region Helper Methods

    private static long _subscriptionSequence = 1;
    private static long _planSequence = 1;

    private static Subscription CreateTestSubscription(long userId = 1)
    {
        return Subscription.Create(
            SubscriptionId.FromSequence(_subscriptionSequence++),
            new UserId(userId),
            PlanId.FromSequence(_planSequence++),
            "Pro",
            BillingCycle.Monthly,
            false).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCancelSubscription()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new CancelSubscriptionCommand("U00000001", "Switching to competitor");

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancellationReason.Should().Be("Switching to competitor");

        _subscriptionRepository.Received(1).Update(subscription);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutReason_ShouldCancelSubscription()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new CancelSubscriptionCommand("U00000001", null);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancellationReason.Should().BeNull();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelSubscriptionCommand("invalid-user-id", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Not Found Cases

    [Fact]
    public async Task Handle_WhenNoActiveSubscription_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelSubscriptionCommand("U00000001", null);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Domain Validation Failures

    [Fact]
    public async Task Handle_WhenSubscriptionAlreadyExpired_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Expire(); // Make it expired

        var command = new CancelSubscriptionCommand("U00000001", null);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotCancelExpired");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldFetchActiveSubscription()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new CancelSubscriptionCommand("U00000001", null);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _subscriptionRepository.Received(1).GetActiveByUserIdAsync(
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUpdateSubscription()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new CancelSubscriptionCommand("U00000001", null);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _subscriptionRepository.Received(1).Update(subscription);
    }

    #endregion
}
