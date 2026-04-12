using GeneFlow.ApiNet2.Application.Subscriptions.Commands.ChangePlan;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Application.Subscriptions.Commands;

/// <summary>
/// Unit tests for ChangePlanCommandHandler.
/// </summary>
public class ChangePlanCommandHandlerTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly ISubscriptionUnitOfWork _unitOfWork = Substitute.For<ISubscriptionUnitOfWork>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly ChangePlanCommandHandler _handler;

    public ChangePlanCommandHandlerTests()
    {
        _handler = new ChangePlanCommandHandler(
            _subscriptionRepository,
            _unitOfWork,
            _planRepository);
    }

    #region Helper Methods

    private static long _planSequence = 1;
    private static long _subscriptionSequence = 1;

    private static Plan CreateTestPlan(string name = "Pro", decimal monthlyPrice = 29m, bool isActive = true)
    {
        var planId = PlanId.FromSequence(_planSequence++);
        var planName = PlanName.Create(name).Value;
        var pricing = PlanPricing.Create(monthlyPrice, monthlyPrice * 10, "EUR").Value;
        var limits = PlanLimits.Create(10, 500, 10).Value;
        var plan = Plan.Create(planId, planName, "Test plan", pricing, limits).Value;

        if (!isActive)
            plan.Deactivate();

        return plan;
    }

    private static Plan CreateFreePlan()
    {
        var planId = PlanId.FromSequence(_planSequence++);
        var planName = PlanName.Create("Free").Value;
        var pricing = PlanPricing.Free();
        var limits = PlanLimits.FreeTier();
        return Plan.Create(planId, planName, "Free tier", pricing, limits, isDefault: true).Value;
    }

    private static Subscription CreateTestSubscription(Plan plan)
    {
        return Subscription.Create(
            SubscriptionId.FromSequence(_subscriptionSequence++),
            new UserId(1),
            plan.Id,
            plan.Name.Value,
            BillingCycle.Monthly,
            false).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidUpgrade_ShouldChangePlan()
    {
        // Arrange
        var currentPlan = CreateTestPlan("Pro", 29m);
        var newPlan = CreateTestPlan("Enterprise", 99m);
        var subscription = CreateTestSubscription(currentPlan);

        var command = new ChangePlanCommand(
            "U00000001",
            newPlan.Id.ToString(),
            BillingCycle.Monthly.Id);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(newPlan.Id, Arg.Any<CancellationToken>())
            .Returns(newPlan);

        _planRepository
            .GetByIdAsync(currentPlan.Id, Arg.Any<CancellationToken>())
            .Returns(currentPlan);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlanName.Should().Be("Enterprise");

        _subscriptionRepository.Received(1).Update(subscription);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBillingCycleChange_ShouldUpdateBillingCycle()
    {
        // Arrange
        var currentPlan = CreateTestPlan("Pro", 29m);
        var newPlan = CreateTestPlan("Pro", 29m);
        var subscription = CreateTestSubscription(currentPlan);

        var command = new ChangePlanCommand(
            "U00000001",
            newPlan.Id.ToString(),
            BillingCycle.Annual.Id);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(newPlan.Id, Arg.Any<CancellationToken>())
            .Returns(newPlan);

        _planRepository
            .GetByIdAsync(currentPlan.Id, Arg.Any<CancellationToken>())
            .Returns(currentPlan);

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
        var command = new ChangePlanCommand(
            "invalid-user-id",
            "L99999999",
            BillingCycle.Monthly.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidPlanId_ShouldReturnFailure()
    {
        // Arrange
        var command = new ChangePlanCommand(
            "U00000001",
            "invalid-plan-id",
            BillingCycle.Monthly.Id);

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
        var command = new ChangePlanCommand(
            "U00000001",
            "L99999999",
            999);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidBillingCycle");
    }

    #endregion

    #region Not Found Cases

    [Fact]
    public async Task Handle_WhenNoActiveSubscription_ShouldReturnFailure()
    {
        // Arrange
        var command = new ChangePlanCommand(
            "U00000001",
            "L99999999",
            BillingCycle.Monthly.Id);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenNewPlanNotFound_ShouldReturnFailure()
    {
        // Arrange
        var currentPlan = CreateTestPlan();
        var subscription = CreateTestSubscription(currentPlan);
        var newPlanId = PlanId.FromSequence(99999);

        var command = new ChangePlanCommand(
            "U00000001",
            newPlanId.ToString(),
            BillingCycle.Monthly.Id);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(newPlanId, Arg.Any<CancellationToken>())
            .Returns((Plan?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PlanNotFound");
    }

    [Fact]
    public async Task Handle_WhenNewPlanNotActive_ShouldReturnFailure()
    {
        // Arrange
        var currentPlan = CreateTestPlan("Pro", 29m);
        var newPlan = CreateTestPlan("Enterprise", 99m, isActive: false);
        var subscription = CreateTestSubscription(currentPlan);

        var command = new ChangePlanCommand(
            "U00000001",
            newPlan.Id.ToString(),
            BillingCycle.Monthly.Id);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(newPlan.Id, Arg.Any<CancellationToken>())
            .Returns(newPlan);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PlanNotActive");
    }

    #endregion

    #region Domain Validation Failures

    [Fact]
    public async Task Handle_DowngradeToFreeDuringPaidPeriod_ShouldReturnFailure()
    {
        // Arrange
        var currentPlan = CreateTestPlan("Pro", 29m);
        var freePlan = CreateFreePlan();
        var subscription = CreateTestSubscription(currentPlan);

        var command = new ChangePlanCommand(
            "U00000001",
            freePlan.Id.ToString(),
            BillingCycle.Monthly.Id);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(freePlan.Id, Arg.Any<CancellationToken>())
            .Returns(freePlan);

        _planRepository
            .GetByIdAsync(currentPlan.Id, Arg.Any<CancellationToken>())
            .Returns(currentPlan);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotDowngradeToFreeDuringPaidPeriod");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldFetchCurrentPlanForPriceComparison()
    {
        // Arrange
        var currentPlan = CreateTestPlan("Pro", 29m);
        var newPlan = CreateTestPlan("Enterprise", 99m);
        var subscription = CreateTestSubscription(currentPlan);

        var command = new ChangePlanCommand(
            "U00000001",
            newPlan.Id.ToString(),
            BillingCycle.Monthly.Id);

        _subscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _planRepository
            .GetByIdAsync(newPlan.Id, Arg.Any<CancellationToken>())
            .Returns(newPlan);

        _planRepository
            .GetByIdAsync(currentPlan.Id, Arg.Any<CancellationToken>())
            .Returns(currentPlan);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _planRepository.Received(1).GetByIdAsync(currentPlan.Id, Arg.Any<CancellationToken>());
        await _planRepository.Received(1).GetByIdAsync(newPlan.Id, Arg.Any<CancellationToken>());
    }

    #endregion
}
