using GeneFlow.ApiNet2.Application.Plans.Queries.GetPlanById;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Plans.Queries;

/// <summary>
/// Unit tests for GetPlanByIdQueryHandler.
/// </summary>
public class GetPlanByIdQueryHandlerTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly GetPlanByIdQueryHandler _handler;

    public GetPlanByIdQueryHandlerTests()
    {
        _handler = new GetPlanByIdQueryHandler(_planRepository);
    }

    #region Helper Methods

    private static long _planSequence = 1;

    private static Plan CreateTestPlan(string name = "Pro", decimal monthlyPrice = 29m)
    {
        var planId = PlanId.FromSequence(_planSequence++);
        var planName = PlanName.Create(name).Value;
        var pricing = PlanPricing.Create(monthlyPrice, monthlyPrice * 10, "EUR").Value;
        var limits = PlanLimits.Create(10, 500, 10).Value;

        return Plan.Create(planId, planName, $"{name} plan", pricing, limits).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidPlanId_ShouldReturnPlan()
    {
        // Arrange
        var plan = CreateTestPlan("Enterprise", 99);
        var query = new GetPlanByIdQuery(plan.Id.ToString());

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Enterprise");
        result.Value.MonthlyPrice.Should().Be(99);
    }

    [Fact]
    public async Task Handle_ShouldMapPlanDtoCorrectly()
    {
        // Arrange
        var plan = CreateTestPlan("Pro", 29);
        var query = new GetPlanByIdQuery(plan.Id.ToString());

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.PlanId.Should().Be(plan.Id.ToString());
        dto.Name.Should().Be("Pro");
        dto.Description.Should().Be("Pro plan");
        dto.MonthlyPrice.Should().Be(29);
        dto.AnnualPrice.Should().Be(290);
        dto.Currency.Should().Be("EUR");
        dto.MaxStudies.Should().Be(10);
        dto.MaxTracesPerMonth.Should().Be(500);
        dto.MaxMembersPerStudy.Should().Be(10);
        dto.IsActive.Should().BeTrue();
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WhenPlanNotFound_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPlanByIdQuery("L00000999");

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns((Plan?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidPlanIdFormat_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPlanByIdQuery("invalid-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("X00000001")]
    [InlineData("L123")]
    public async Task Handle_WithMalformedPlanId_ShouldReturnFailure(string planId)
    {
        // Arrange
        var query = new GetPlanByIdQuery(planId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce()
    {
        // Arrange
        var plan = CreateTestPlan();
        var query = new GetPlanByIdQuery(plan.Id.ToString());

        _planRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _planRepository.Received(1).GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
