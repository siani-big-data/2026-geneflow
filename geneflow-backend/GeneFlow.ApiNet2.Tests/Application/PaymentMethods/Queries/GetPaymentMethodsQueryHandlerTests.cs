using GeneFlow.ApiNet2.Application.PaymentMethods.Queries.GetPaymentMethods;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;

namespace GeneFlow.ApiNet2.Tests.Application.PaymentMethods.Queries;

/// <summary>
/// Unit tests for GetPaymentMethodsQueryHandler.
/// </summary>
public class GetPaymentMethodsQueryHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>();
    private readonly GetPaymentMethodsQueryHandler _handler;

    public GetPaymentMethodsQueryHandlerTests()
    {
        _handler = new GetPaymentMethodsQueryHandler(_repository);
    }

    #region Helper Methods

    private static PaymentMethod CreateTestPaymentMethod(
        long id = 1,
        long userId = 1,
        string stripeId = "pm_test123",
        string brand = "visa",
        string last4 = "4242",
        bool isDefault = false)
    {
        var paymentMethodId = PaymentMethodId.FromSequence(id);
        var userIdValue = new UserId(userId);
        return PaymentMethod.Create(
            paymentMethodId,
            userIdValue,
            stripeId,
            brand,
            last4,
            12,
            2030,
            isDefault).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidUserId_ShouldReturnMethods()
    {
        // Arrange
        var paymentMethod1 = CreateTestPaymentMethod(id: 1, isDefault: true);
        var paymentMethod2 = CreateTestPaymentMethod(id: 2, stripeId: "pm_test456", last4: "1234", isDefault: false);
        var query = new GetPaymentMethodsQuery("U00000001");

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { paymentMethod1, paymentMethod2 });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].IsDefault.Should().BeTrue();
        result.Value[0].Last4.Should().Be("4242");
        result.Value[1].IsDefault.Should().BeFalse();
        result.Value[1].Last4.Should().Be("1234");
    }

    [Fact]
    public async Task Handle_WithNoMethods_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetPaymentMethodsQuery("U00000001");

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectDtoProperties()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(
            id: 1,
            brand: "mastercard",
            last4: "5678",
            isDefault: true);
        var query = new GetPaymentMethodsQuery("U00000001");

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { paymentMethod });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.Id.Should().Be(paymentMethod.Id.ToString());
        dto.Brand.Should().Be("mastercard");
        dto.Last4.Should().Be("5678");
        dto.ExpiryMonth.Should().Be(12);
        dto.ExpiryYear.Should().Be(2030);
        dto.IsDefault.Should().BeTrue();
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnError()
    {
        // Arrange
        var query = new GetPaymentMethodsQuery("invalid-user-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Repository Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectUserId()
    {
        // Arrange
        var query = new GetPaymentMethodsQuery("U00000001");

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByUserIdAsync(
            Arg.Is<UserId>(id => id.Value == 1),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
