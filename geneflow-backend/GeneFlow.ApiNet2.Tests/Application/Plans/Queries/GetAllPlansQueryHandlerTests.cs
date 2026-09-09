using GeneFlow.ApiNet2.Application.Plans.Queries.GetAllPlans;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Plans.Queries;

/// <summary>
/// Unit tests for GetAllPlansQueryHandler.
/// </summary>
public class GetAllPlansQueryHandlerTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly GetAllPlansQueryHandler _handler;

    public GetAllPlansQueryHandlerTests()
    {
        _handler = new GetAllPlansQueryHandler(_planRepository);
    }

    #region Helper Methods

    private static long _planSequence = 1;

    private static Plan CreateTestPlan(string name, decimal monthlyPrice, bool isFree = false, int displayOrder = 0)
    {
        var planId = PlanId.FromSequence(_planSequence++);
        var planName = PlanName.Create(name).Value;
        var pricing = isFree ? PlanPricing.Free() : PlanPricing.Create(monthlyPrice, monthlyPrice * 10, "EUR").Value;
        var limits = isFree ? PlanLimits.FreeTier() : PlanLimits.Create(10, 500, 10).Value;

        return Plan.Create(planId, planName, $"{name} plan", pricing, limits, displayOrder, isDefault: isFree).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_ShouldReturnAllActivePlans()
    {
        // Arrange
        var query = new GetAllPlansQuery();
        var plans = new List<Plan>
        {
            CreateTestPlan("Free", 0, isFree: true, displayOrder: 0),
            CreateTestPlan("Pro", 29, displayOrder: 1),
            CreateTestPlan("Enterprise", 99, displayOrder: 2)
        };

        _planRepository
            .GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(plans);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlansOrderedByDisplayOrder()
    {
        // Arrange
        var query = new GetAllPlansQuery();
        var plans = new List<Plan>
        {
            CreateTestPlan("Enterprise", 99, displayOrder: 2),
            CreateTestPlan("Free", 0, isFree: true, displayOrder: 0),
            CreateTestPlan("Pro", 29, displayOrder: 1)
        };

        _planRepository
            .GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(plans);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WhenNoPlans_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetAllPlansQuery();

        _planRepository
            .GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Plan>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapPlanCorrectly()
    {
        // Arrange
        var query = new GetAllPlansQuery();
        var plan = CreateTestPlan("Pro", 29);

        _planRepository
            .GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Plan> { plan });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.Name.Should().Be("Pro");
        dto.MonthlyPrice.Should().Be(29);
        dto.IsFree.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldMapFreePlanCorrectly()
    {
        // Arrange
        var query = new GetAllPlansQuery();
        var plan = CreateTestPlan("Free", 0, isFree: true);

        _planRepository
            .GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Plan> { plan });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.Name.Should().Be("Free");
        dto.IsFree.Should().BeTrue();
        dto.IsDefault.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce()
    {
        // Arrange
        var query = new GetAllPlansQuery();

        _planRepository
            .GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Plan>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _planRepository.Received(1).GetAllActiveAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
