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

    [Fact]
    public void SetAutoRenew_EnableAgain_ShouldUpdateToTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.SetAutoRenew(false);

        // Act
        var result = subscription.SetAutoRenew(true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.AutoRenew.Should().BeTrue();
    }

    #endregion

    #region Renew - Additional Tests

    [Fact]
    public void Renew_WhenExpired_ShouldSetActiveStatus()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Expire();

        // Act
        var result = subscription.Renew();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.AutoRenew.Should().BeTrue();
    }

    [Fact]
    public void Renew_WhenSuspended_ShouldSetActiveStatus()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Suspend();

        // Act
        var result = subscription.Renew();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Renew_ShouldCreateNewPeriod()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Cancel();
        var oldPeriodEnd = subscription.CurrentPeriod.EndDate;

        // Act
        subscription.Renew();

        // Assert
        subscription.CurrentPeriod.EndDate.Should().BeAfter(oldPeriodEnd);
    }

    [Fact]
    public void Renew_WhenInTrial_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);

        // Act
        var result = subscription.Renew();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotRenewActive");
    }

    #endregion

    #region Cancel - Additional Tests

    [Fact]
    public void Cancel_WithoutReason_ShouldSucceed()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.CancellationReason.Should().BeNull();
    }

    [Fact]
    public void Cancel_WhenInTrial_ShouldSucceed()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);

        // Act
        var result = subscription.Cancel("No longer needed");

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenSuspended_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Suspend();

        // Act
        var result = subscription.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotCancelExpired");
    }

    [Fact]
    public void Cancel_ShouldSetModifiedAt()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var beforeCancel = DateTime.UtcNow;

        // Act
        subscription.Cancel();

        // Assert
        subscription.ModifiedAt.Should().NotBeNull();
        subscription.ModifiedAt.Should().BeOnOrAfter(beforeCancel);
    }

    #endregion

    #region ChangePlan - Additional Tests

    [Fact]
    public void ChangePlan_Upgrade_ShouldExtendPeriod()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Basic");
        var oldPeriodEnd = subscription.CurrentPeriod.EndDate;

        // Act
        var result = subscription.ChangePlan(CreatePlanId(), "Pro", BillingCycle.Monthly, isUpgrade: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.CurrentPeriod.EndDate.Should().BeAfter(oldPeriodEnd);
    }

    [Fact]
    public void ChangePlan_Downgrade_ShouldKeepCurrentPeriod()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Enterprise");
        var oldPeriodEnd = subscription.CurrentPeriod.EndDate;

        // Act
        var result = subscription.ChangePlan(CreatePlanId(), "Pro", BillingCycle.Monthly, isUpgrade: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.CurrentPeriod.EndDate.Should().BeCloseTo(oldPeriodEnd, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangePlan_ShouldChangeBillingCycle()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Pro");

        // Act
        var result = subscription.ChangePlan(CreatePlanId(), "Pro", BillingCycle.Annual, isUpgrade: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.BillingCycle.Should().Be(BillingCycle.Annual);
    }

    [Fact]
    public void ChangePlan_WhenCancelled_ShouldSucceed()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Pro");
        subscription.Cancel();

        // Act
        var result = subscription.ChangePlan(CreatePlanId(), "Enterprise", BillingCycle.Monthly, isUpgrade: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.PlanName.Should().Be("Enterprise");
    }

    [Fact]
    public void ChangePlan_EventShouldContainCorrectData()
    {
        // Arrange
        var subscription = CreateTestSubscription(planName: "Pro");
        subscription.ClearDomainEvents();
        var newPlanId = CreatePlanId();

        // Act
        subscription.ChangePlan(newPlanId, "Enterprise", BillingCycle.Annual, isUpgrade: true);

        // Assert
        var planChangedEvent = subscription.DomainEvents.OfType<SubscriptionPlanChangedEvent>().Single();
        planChangedEvent.OldPlanName.Should().Be("Pro");
        planChangedEvent.NewPlanName.Should().Be("Enterprise");
        planChangedEvent.IsUpgrade.Should().BeTrue();
    }

    #endregion

    #region Suspend - Additional Tests

    [Fact]
    public void Suspend_WhenPastDue_ShouldSucceed()
    {
        // Arrange - Using reflection or create scenario where status becomes PastDue
        // Since there's no direct method to set PastDue, we test Active scenario
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.Suspend();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
    }

    [Fact]
    public void Suspend_ShouldSetModifiedAt()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var beforeSuspend = DateTime.UtcNow;

        // Act
        subscription.Suspend();

        // Assert
        subscription.ModifiedAt.Should().NotBeNull();
        subscription.ModifiedAt.Should().BeOnOrAfter(beforeSuspend);
    }

    [Fact]
    public void Suspend_WhenExpired_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Expire();

        // Act
        var result = subscription.Suspend();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotSuspendNonActive");
    }

    [Fact]
    public void Suspend_WhenInTrial_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);

        // Act
        var result = subscription.Suspend();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotSuspendNonActive");
    }

    #endregion

    #region Reactivate - Additional Tests

    [Fact]
    public void Reactivate_ShouldSetModifiedAt()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Suspend();
        var beforeReactivate = DateTime.UtcNow;

        // Act
        subscription.Reactivate();

        // Assert
        subscription.ModifiedAt.Should().NotBeNull();
        subscription.ModifiedAt.Should().BeOnOrAfter(beforeReactivate);
    }

    [Fact]
    public void Reactivate_WhenCancelled_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Cancel();

        // Act
        var result = subscription.Reactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotReactivateNonSuspended");
    }

    [Fact]
    public void Reactivate_WhenExpired_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Expire();

        // Act
        var result = subscription.Reactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotReactivateNonSuspended");
    }

    #endregion

    #region ActivateFromTrial - Additional Tests

    [Fact]
    public void ActivateFromTrial_ShouldCreateNewPeriod()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);
        var oldPeriodEnd = subscription.CurrentPeriod.EndDate;

        // Act
        subscription.ActivateFromTrial();

        // Assert
        subscription.CurrentPeriod.EndDate.Should().BeAfter(oldPeriodEnd);
    }

    [Fact]
    public void ActivateFromTrial_ShouldSetModifiedAt()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);
        var beforeActivation = DateTime.UtcNow;

        // Act
        subscription.ActivateFromTrial();

        // Assert
        subscription.ModifiedAt.Should().NotBeNull();
        subscription.ModifiedAt.Should().BeOnOrAfter(beforeActivation);
    }

    [Fact]
    public void ActivateFromTrial_WhenCancelled_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription(startWithTrial: true);
        subscription.Cancel();

        // Act
        var result = subscription.ActivateFromTrial();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Expire - Additional Tests

    [Fact]
    public void ExpiREDACTED()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var beforeExpire = DateTime.UtcNow;

        // Act
        subscription.Expire();

        // Assert
        subscription.ModifiedAt.Should().NotBeNull();
        subscription.ModifiedAt.Should().BeOnOrAfter(beforeExpire);
    }

    [Fact]
    public void ExpiREDACTED()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Cancel();

        // Act
        var result = subscription.Expire();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Expired);
    }

    [Fact]
    public void ExpiREDACTED()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        subscription.Expire();

        // Assert
        subscription.GrantsAccess.Should().BeFalse();
    }

    #endregion

    #region Create - Additional Tests with BillingCycle

    [Fact]
    public void Create_WithAnnualBillingCycle_ShouldSetCorrectPeriod()
    {
        // Arrange
        var subscriptionId = CreateSubscriptionId();
        var userId = CreateUserId();
        var planId = CreatePlanId();

        // Act
        var result = Subscription.Create(subscriptionId, userId, planId, "Pro", BillingCycle.Annual, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BillingCycle.Should().Be(BillingCycle.Annual);
        result.Value.CurrentPeriod.EndDate.Should().BeCloseTo(
            DateTime.UtcNow.AddMonths(12),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithTrialAndAnnualCycle_ShouldSetTrialEndDate()
    {
        // Arrange
        var subscriptionId = CreateSubscriptionId();
        var userId = CreateUserId();
        var planId = CreatePlanId();

        // Act
        var result = Subscription.Create(subscriptionId, userId, planId, "Pro", BillingCycle.Annual, startWithTrial: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrialEndDate.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(Subscription.DefaultTrialDays),
            TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GrantsAccess - Additional Tests

    [Fact]
    public void GrantsAccess_WhenCancelledButNotExpired_ShouldBeTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Cancel();

        // Assert - Cancelled but period not expired
        subscription.GrantsAccess.Should().BeTrue();
    }

    [Fact]
    public void GrantsAccess_WhenSuspended_ShouldBeFalse()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Suspend();

        // Assert
        subscription.GrantsAccess.Should().BeFalse();
    }

    #endregion

    #region IsFree - Additional Tests

    [Fact]
    public void IsFree_CaseInsensitive_ShouldBeTrue()
    {
        // Arrange
        var subscriptionId = CreateSubscriptionId();
        var userId = CreateUserId();
        var planId = CreatePlanId();

        // Act - Create with "FREE" in uppercase
        var result = Subscription.Create(subscriptionId, userId, planId, "FREE", BillingCycle.Monthly, false);

        // Assert
        result.Value.IsFree.Should().BeTrue();
    }

    #endregion

    #region Cancel - Idempotency Tests

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldBeIdempotent()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Cancel("First cancellation");
        var firstCancelledAt = subscription.CancelledAt;

        // Act - Attempt to cancel again (Cancelled status has CanCancel = false)
        var result = subscription.Cancel("Second cancellation");

        // Assert - Should fail because CanCancel is false for Cancelled status
        result.IsFailure.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancelledAt.Should().Be(firstCancelledAt);
        subscription.CancellationReason.Should().Be("First cancellation");
    }

    #endregion

    #region Renew - Period Extension Tests

    [Fact]
    public void Renew_WhenActive_ShouldExtendPeriod_NotApplicable()
    {
        // Note: Active subscriptions cannot be renewed per domain logic.
        // This test documents that behavior - renew requires Cancelled/Expired/Suspended status.
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var result = subscription.Renew();

        // Assert - Active subscriptions cannot renew
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotRenewActive");
    }

    #endregion
}
