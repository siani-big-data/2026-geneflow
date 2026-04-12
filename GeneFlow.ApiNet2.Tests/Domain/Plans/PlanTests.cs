using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.Enumerations;
using GeneFlow.ApiNet2.Domain.Plans.Events;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Plans;

/// <summary>
/// Unit tests for the Plan aggregate root.
/// </summary>
public class PlanTests
{
    #region Helper Methods

    private static long _planSequence = 1;

    private static Plan CreateTestPlan(
        string name = "Pro",
        decimal monthlyPrice = 29m,
        decimal annualPrice = 290m,
        int maxStudies = 10,
        int maxTracesPerMonth = 500,
        int maxMembersPerStudy = 10)
    {
        var planId = PlanId.FromSequence(_planSequence++);
        var planName = PlanName.Create(name).Value;
        var pricing = PlanPricing.Create(monthlyPrice, annualPrice, "EUR").Value;
        var limits = PlanLimits.Create(maxStudies, maxTracesPerMonth, maxMembersPerStudy).Value;

        return Plan.Create(planId, planName, "Test description", pricing, limits).Value;
    }

    private static Plan CreateFreePlan()
    {
        var planId = PlanId.FromSequence(_planSequence++);
        var planName = PlanName.Create("Free").Value;
        var pricing = PlanPricing.Free();
        var limits = PlanLimits.FreeTier();

        return Plan.Create(planId, planName, "Free tier", pricing, limits, isDefault: true).Value;
    }

    #endregion

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldReturnPlan()
    {
        // Arrange
        var planId = PlanId.FromSequence(100);
        var name = PlanName.Create("Enterprise").Value;
        var pricing = PlanPricing.Create(99, 990, "EUR").Value;
        var limits = PlanLimits.CreateUnlimited();

        // Act
        var result = Plan.Create(planId, name, "Enterprise plan", pricing, limits);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Pricing.Should().Be(pricing);
        result.Value.Limits.Should().Be(limits);
        result.Value.Description.Should().Be("Enterprise plan");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetIsActiveToTrue()
    {
        // Act
        var plan = CreateTestPlan();

        // Assert
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDefaultFlag_ShouldSetIsDefault()
    {
        // Arrange
        var planId = PlanId.FromSequence(101);
        var name = PlanName.Create("Free").Value;
        var pricing = PlanPricing.Free();
        var limits = PlanLimits.FreeTier();

        // Act
        var result = Plan.Create(planId, name, null, pricing, limits, isDefault: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRaisePlanCreatedEvent()
    {
        // Act
        var plan = CreateTestPlan();

        // Assert
        plan.DomainEvents.Should().ContainSingle(e => e is PlanCreatedEvent);
    }

    [Fact]
    public void Create_ShouldGenerateNewPlanId()
    {
        // Act
        var plan = CreateTestPlan();

        // Assert
        plan.Id.Should().NotBeNull();
        plan.Id.Value.Should().BeGreaterThan(0);
    }

    #endregion

    #region IsFree

    [Fact]
    public void IsFree_WhenPricingIsFree_ShouldBeTrue()
    {
        // Arrange
        var plan = CreateFreePlan();

        // Assert
        plan.IsFree.Should().BeTrue();
    }

    [Fact]
    public void IsFree_WhenPricingIsPaid_ShouldBeFalse()
    {
        // Arrange
        var plan = CreateTestPlan();

        // Assert
        plan.IsFree.Should().BeFalse();
    }

    #endregion

    #region UpdateDetails

    [Fact]
    public void UpdateDetails_ShouldUpdateNameDescriptionAndOrder()
    {
        // Arrange
        var plan = CreateTestPlan();
        var newName = PlanName.Create("Premium").Value;

        // Act
        var result = plan.UpdateDetails(newName, "New description", 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.Name.Should().Be(newName);
        plan.Description.Should().Be("New description");
        plan.DisplayOrder.Should().Be(5);
    }

    #endregion

    #region UpdatePricing

    [Fact]
    public void UpdatePricing_ShouldUpdatePricing()
    {
        // Arrange
        var plan = CreateTestPlan();
        var newPricing = PlanPricing.Create(49, 490, "EUR").Value;

        // Act
        var result = plan.UpdatePricing(newPricing);

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.Pricing.Should().Be(newPricing);
    }

    #endregion

    #region UpdateLimits

    [Fact]
    public void UpdateLimits_ShouldUpdateLimits()
    {
        // Arrange
        var plan = CreateTestPlan();
        var newLimits = PlanLimits.CreateUnlimited();

        // Act
        var result = plan.UpdateLimits(newLimits);

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.Limits.Should().Be(newLimits);
    }

    #endregion

    #region Activate/Deactivate

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.Deactivate();

        // Act
        var result = plan.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var plan = CreateTestPlan();

        // Act
        var result = plan.Deactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenIsDefault_ShouldReturnFailure()
    {
        // Arrange
        var plan = CreateFreePlan(); // Free plan is default

        // Act
        var result = plan.Deactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotDeactivateDefaultPlan");
    }

    #endregion

    #region SetAsDefault/RemoveDefaultStatus

    [Fact]
    public void SetAsDefault_ShouldSetIsDefaultToTrue()
    {
        // Arrange
        var plan = CreateTestPlan();

        // Act
        var result = plan.SetAsDefault();

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void RemoveDefaultStatus_ShouldSetIsDefaultToFalse()
    {
        // Arrange
        var plan = CreateFreePlan();

        // Act
        var result = plan.RemoveDefaultStatus();

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.IsDefault.Should().BeFalse();
    }

    #endregion

    #region Features

    [Fact]
    public void AddFeatuREDACTED()
    {
        // Arrange
        var plan = CreateTestPlan();

        // Act
        var result = plan.AddFeature(PlanFeature.CopilotAccess);

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.Features.Should().Contain(PlanFeature.CopilotAccess);
        plan.HasFeature(PlanFeature.CopilotAccess).Should().BeTrue();
    }

    [Fact]
    public void AddFeatuREDACTED()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.AddFeature(PlanFeature.CopilotAccess);

        // Act
        var result = plan.AddFeature(PlanFeature.CopilotAccess);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FeatureAlreadyExists");
    }

    [Fact]
    public void RemoveFeatuREDACTED()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.AddFeature(PlanFeature.CopilotAccess);

        // Act
        var result = plan.RemoveFeature(PlanFeature.CopilotAccess);

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.Features.Should().NotContain(PlanFeature.CopilotAccess);
        plan.HasFeature(PlanFeature.CopilotAccess).Should().BeFalse();
    }

    [Fact]
    public void RemoveFeatuREDACTED()
    {
        // Arrange
        var plan = CreateTestPlan();

        // Act
        var result = plan.RemoveFeature(PlanFeature.CopilotAccess);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FeatureNotFound");
    }

    [Fact]
    public void HasFeatuREDACTED()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.AddFeature(PlanFeature.ApiAccess);

        // Assert
        plan.HasFeature(PlanFeature.ApiAccess).Should().BeTrue();
    }

    [Fact]
    public void HasFeatuREDACTED()
    {
        // Arrange
        var plan = CreateTestPlan();

        // Assert
        plan.HasFeature(PlanFeature.ApiAccess).Should().BeFalse();
    }

    #endregion
}
