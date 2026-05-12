using GeneFlow.ApiNet2.Application.Subscriptions.Commands.CreateSubscription;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using GeneFlow.ApiNet2.Domain.Subscriptions.Events;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Subscriptions.Commands;

/// <summary>
/// Unit tests for CreateSubscriptionCommandHandler.
/// </summary>
public class CreateSubscriptionCommandHandlerTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly ISubscriptionUnitOfWork _unitOfWork = Substitute.For<ISubscriptionUnitOfWork>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly CreateSubscriptionCommandHandler _handler;

    public CreateSubscriptionCommandHandlerTests()
    {
        _sequenceGenerator
            .NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(1L));

        _handler = new CreateSubscriptionCommandHandler(
            _subscriptionRepository,
            _unitOfWork,
            _planRepository,
            _sequenceGenerator);
    }

    #region Helper Methods

    private static long _planSequence = 1;

    private static Plan CreateTestPlan(bool isActive = true, bool isFree = false)
    {
        var planId = PlanId.FromSequence(_planSequence++);
        var name = PlanName.Create(isFree ? "Free" : "Pro").Value;
        var pricing = isFree ? PlanPricing.Free() : PlanPricing.Create(29, 290, "EUR").Value;
        var limits = isFree ? PlanLimits.FreeTier() : PlanLimits.Create(10, 500, 10).Value;

        return Plan.Create(planId, name, "Test plan", pricing, limits, isDefault: isFree).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateSubscription()
    {
        // Arrange
        var plan = CreateTestPlan();
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            false);

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlanName.Should().Be("Pro");
        result.Value.Status.Should().Be("Active");

        await _subscriptionRepository.Received(1).AddAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTrial_ShouldCreateTrialSubscription()
    {
        // Arrange
        var plan = CreateTestPlan();
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            true); // startWithTrial = true

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Trial");
        result.Value.IsInTrial.Should().BeTrue();
        result.Value.TrialEndDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithFreePlan_ShouldCreateFreeSubscription()
    {
        // Arrange
        var plan = CreateTestPlan(isFree: true);
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            false);

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFree.Should().BeTrue();
        result.Value.PlanName.Should().Be("Free");
    }

    [Fact]
    public async Task Handle_WithAnnualBillingCycle_ShouldCreateAnnualSubscription()
    {
        // Arrange
        var plan = CreateTestPlan();
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Annual.Id,
            false);

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BillingCycle.Should().Be("Annual");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateSubscriptionCommand(
            "invalid-user-id",
            "L99999999",
            BillingCycle.Monthly.Id,
            false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidPlanId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateSubscriptionCommand(
            "U00000001",
            "invalid-plan-id",
            BillingCycle.Monthly.Id,
            false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PlanNotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidBillingCycle_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateSubscriptionCommand(
            "U00000001",
            "L99999999",
            999, // Invalid billing cycle ID
            false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidBillingCycle");
    }

    #endregion

    #region Conflict Cases

    [Fact]
    public async Task Handle_WhenUserAlreadyHasActiveSubscription_ShouldReturnFailure()
    {
        // Arrange
        var plan = CreateTestPlan();
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            false);

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(true); // User already has active subscription

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("UserAlreadyHasActiveSubscription");
    }

    [Fact]
    public async Task Handle_WhenPlanNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateSubscriptionCommand(
            "U00000001",
            "L99999999",
            BillingCycle.Monthly.Id,
            false);

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns((Plan?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PlanNotFound");
    }

    [Fact]
    public async Task Handle_WhenPlanNotActive_ShouldReturnFailure()
    {
        // Arrange
        var plan = CreateTestPlan(isActive: true);
        plan.Deactivate();

        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            false);

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PlanNotActive");
    }

    #endregion

    #region Domain Events

    [Fact]
    public async Task Handle_ShouldRaiseSubscriptionCreatedEvent()
    {
        // Arrange
        var plan = CreateTestPlan();
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            false);

        Subscription? capturedSubscription = null;

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _subscriptionRepository
            .When(x => x.AddAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedSubscription = callInfo.Arg<Subscription>());

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.DomainEvents.Should().ContainSingle();
        capturedSubscription.DomainEvents.First().Should().BeOfType<SubscriptionCreatedEvent>();

        var domainEvent = capturedSubscription.DomainEvents.First() as SubscriptionCreatedEvent;
        domainEvent!.SubscriptionId.Should().Be(capturedSubscription.Id);
        domainEvent.UserId.Value.Should().Be(1);
        domainEvent.PlanId.Should().Be(plan.Id);
    }

    [Fact]
    public async Task Handle_WithTrial_ShouldRaiseSubscriptionCreatedEvent()
    {
        // Arrange
        var plan = CreateTestPlan();
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            true); // startWithTrial

        Subscription? capturedSubscription = null;

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _subscriptionRepository
            .When(x => x.AddAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedSubscription = callInfo.Arg<Subscription>());

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SubscriptionCreatedEvent>();
    }

    [Fact]
    public async Task Handle_WithFreePlan_ShouldRaiseSubscriptionCreatedEvent()
    {
        // Arrange
        var plan = CreateTestPlan(isFree: true);
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            false);

        Subscription? capturedSubscription = null;

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _subscriptionRepository
            .When(x => x.AddAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedSubscription = callInfo.Arg<Subscription>());

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SubscriptionCreatedEvent>();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCheckForActiveSubscriptionFirst()
    {
        // Arrange
        var plan = CreateTestPlan();
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            false);

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _subscriptionRepository.Received(1).HasActiveSubscriptionAsync(
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFetchPlan()
    {
        // Arrange
        var plan = CreateTestPlan();
        var command = new CreateSubscriptionCommand(
            "U00000001",
            plan.Id.ToString(),
            BillingCycle.Monthly.Id,
            false);

        _subscriptionRepository
            .HasActiveSubscriptionAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _planRepository.Received(1).GetByIdAsync(
            Arg.Any<PlanId>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
