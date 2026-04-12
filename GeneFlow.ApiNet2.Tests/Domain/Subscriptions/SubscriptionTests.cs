using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using GeneFlow.ApiNet2.Domain.Subscriptions.Events;

namespace GeneFlow.ApiNet2.Tests.Domain.Subscriptions;

/// <summary>
/// Unit tests for the Subscription aggregate root.
/// </summary>
public class SubscriptionTests
{
    #region Helper Methods

    private static long _subscriptionSequence = 1;
    private static long _planSequence = 1;

    private static UserId CreateUserId(long id = 1) => new(id);
    private static SubscriptionId CreateSubscriptionId() => SubscriptionId.FromSequence(_subscriptionSequence++);
    private static PlanId CreatePlanId() => PlanId.FromSequence(_planSequence++);

    private static Subscription CreateTestSubscription(
        long userId = 1,
        string planName = "Pro",
        bool startWithTrial = false)
    {
        return Subscription.Create(
            CreateSubscriptionId(),
            CreateUserId(userId),
            CreatePlanId(),
            planName,
            BillingCycle.Monthly,
            startWithTrial).Value;
    }

    private static Subscription CreateFreeSubscription(long userId = 1)
    {
        return Subscription.CreateFree(CreateSubscriptionId(), CreateUserId(userId), CreatePlanId()).Value;
    }

    #endregion

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldReturnSubscription()
    {
        // Arrange
        var subscriptionId = CreateSubscriptionId();
        var userId = CreateUserId();
        var planId = CreatePlanId();

        // Act
        var result = Subscription.Create(subscriptionId, userId, planId, "Pro", BillingCycle.Monthly, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.PlanId.Should().Be(planId);
        result.Value.PlanName.Should().Be("Pro");
        result.Value.BillingCycle.Should().Be(BillingCycle.Monthly);
        result.Value.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Create_WithTrial_ShouldSetTrialStatus()
    {
        // Act
        var subscription = CreateTestSubscription(startWithTrial: true);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Trial);
        subscription.TrialEndDate.Should().NotBeNull();
        subscription.TrialEndDate.Should().BeAfter(DateTime.UtcNow);
        subscription.IsInTrial.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutTrial_ShouldSetActiveStatus()
    {
        // Act
        var subscription = CreateTestSubscription(startWithTrial: false);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.TrialEndDate.Should().BeNull();
        subscription.IsInTrial.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetAutoRenewToTrue()
    {
        // Act
        var subscription = CreateTestSubscription();

        // Assert
        subscription.AutoRenew.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRaiseSubscriptionCreatedEvent()
    {
        // Act
        var subscription = CreateTestSubscription();

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionCreatedEvent);
    }

    [Fact]
    public void Create_ShouldSetCurrentPeriod()
    {
        // Act
        var subscription = CreateTestSubscription();

        // Assert
        subscription.CurrentPeriod.Should().NotBeNull();
        subscription.CurrentPeriod.IsActive.Should().BeTrue();
    }

    #endregion

    #region CreateFree

    [Fact]
    public void CreateFree_ShouldCreateActiveSubscription()
    {
        // Act
        var subscription = CreateFreeSubscription();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.PlanName.Should().Be("Free");
        subscription.IsFree.Should().BeTrue();
    }

    [Fact]
    public void CreateFree_ShouldSetAutoRenewToFalse()
    {
        // Act
        var subscription = CreateFreeSubscription();

        // Assert
        subscription.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public void CreateFree_ShouldSetVeryLongPeriod()
    {
        // Act
        var subscription = CreateFreeSubscription();

        // Assert
        subscription.CurrentPeriod.EndDate.Should().BeAfter(DateTime.UtcNow.AddYears(99));
    }

    [Fact]
    public void CreateFree_ShouldRaiseSubscriptionCreatedEvent()
    {
        // Act
        var subscription = CreateFreeSubscription();

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionCreatedEvent);
    }

    #endregion

    #region GrantsAccess

    [Fact]
    public void GrantsAccess_WhenActiveAndNotExpired_ShouldBeTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Assert
        subscription.GrantsAccess.Should().BeTrue();
    }

    [Fact]
    public void GrantsAccess_WhenTrialAndNotExpired_ShouldBeTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);

        // Assert
        subscription.GrantsAccess.Should().BeTrue();
    }

    #endregion

    #region IsFree

    [Fact]
    public void IsFree_WhenPlanNameIsFree_ShouldBeTrue()
    {
        // Arrange
        var subscription = CreateFreeSubscription();

        // Assert
        subscription.IsFree.Should().BeTrue();
    }

    [Fact]
    public void IsFree_WhenPlanNameIsNotFree_ShouldBeFalse()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Pro");

        // Assert
        subscription.IsFree.Should().BeFalse();
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_WhenActive_ShouldSetCancelledStatus()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.Cancel("Switching to competitor");

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancelledAt.Should().NotBeNull();
        subscription.CancellationReason.Should().Be("Switching to competitor");
        subscription.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public void Cancel_ShouldRaiseSubscriptionCancelledEvent()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.ClearDomainEvents();

        // Act
        subscription.Cancel("Test reason");

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionCancelledEvent);
    }

    [Fact]
    public void Cancel_WhenExpired_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Expire();

        // Act
        var result = subscription.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotCancelExpired");
    }

    #endregion

    #region Renew

    [Fact]
    public void Renew_WhenCancelled_ShouldSetActiveStatus()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Cancel();

        // Act
        var result = subscription.Renew();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.CancelledAt.Should().BeNull();
        subscription.CancellationReason.Should().BeNull();
        subscription.AutoRenew.Should().BeTrue();
    }

    [Fact]
    public void Renew_ShouldRaiseSubscriptionRenewedEvent()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Cancel();
        subscription.ClearDomainEvents();

        // Act
        subscription.Renew();

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionRenewedEvent);
    }

    [Fact]
    public void Renew_WhenActive_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.Renew();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotRenewActive");
    }

    #endregion

    #region ChangePlan

    [Fact]
    public void ChangePlan_WithValidPlan_ShouldChangePlan()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Pro");
        var newPlanId = CreatePlanId();

        // Act
        var result = subscription.ChangePlan(newPlanId, "Enterprise", BillingCycle.Annual, isUpgrade: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.PlanId.Should().Be(newPlanId);
        subscription.PlanName.Should().Be("Enterprise");
        subscription.BillingCycle.Should().Be(BillingCycle.Annual);
    }

    [Fact]
    public void ChangePlan_ShouldRaiseSubscriptionPlanChangedEvent()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.ClearDomainEvents();

        // Act
        subscription.ChangePlan(CreatePlanId(), "Enterprise", BillingCycle.Annual, isUpgrade: true);

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionPlanChangedEvent);
    }

    [Fact]
    public void ChangePlan_WhenExpired_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Expire();

        // Act
        var result = subscription.ChangePlan(CreatePlanId(), "Enterprise", BillingCycle.Annual, true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotChangePlanOnExpired");
    }

    [Fact]
    public void ChangePlan_DowngradeToFreeDuringPaidPeriod_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Pro");

        // Act
        var result = subscription.ChangePlan(CreatePlanId(), "Free", BillingCycle.Monthly, isUpgrade: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotDowngradeToFreeDuringPaidPeriod");
    }

    #endregion

    #region ActivateFromTrial

    [Fact]
    public void ActivateFromTrial_WhenInTrial_ShouldSetActiveStatus()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);

        // Act
        var result = subscription.ActivateFromTrial();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.TrialEndDate.Should().BeNull();
    }

    [Fact]
    public void ActivateFromTrial_WhenNotInTrial_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: false);

        // Act
        var result = subscription.ActivateFromTrial();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotActivateFromNonTrial");
    }

    #endregion

    #region Expire

    [Fact]
    public void ExpiREDACTED()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.Expire();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Expired);
        subscription.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public void ExpiREDACTED()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.ClearDomainEvents();

        // Act
        subscription.Expire();

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionExpiredEvent);
    }

    #endregion

    #region Suspend/Reactivate

    [Fact]
    public void Suspend_WhenActive_ShouldSetSuspendedStatus()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.Suspend();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
    }

    [Fact]
    public void Suspend_WhenNotActive_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Cancel();

        // Act
        var result = subscription.Suspend();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotSuspendNonActive");
    }

    [Fact]
    public void Reactivate_WhenSuspended_ShouldSetActiveStatus()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Suspend();

        // Act
        var result = subscription.Reactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Reactivate_WhenNotSuspended_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.Reactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotReactivateNonSuspended");
    }

    #endregion

    #region SetAutoRenew

    [Fact]
    public void SetAutoRenew_ShouldUpdateAutoRenewStatus()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.SetAutoRenew(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public void SetAutoRenew_ShouldSetModifiedAt()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var beforeChange = DateTime.UtcNow;

        // Act
        subscription.SetAutoRenew(false);

        // Assert
        subscription.ModifiedAt.Should().NotBeNull();
        subscription.ModifiedAt.Should().BeOnOrAfter(beforeChange);
    }

    #endregion
}
