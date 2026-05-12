using GeneFlow.ApiNet2.Application.PaymentMethods.Queries.GetDefaultPaymentMethod;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;

namespace GeneFlow.ApiNet2.Tests.Application.PaymentMethods.Queries;

/// <summary>
/// Unit tests for GetDefaultPaymentMethodQueryHandler.
/// </summary>
public class GetDefaultPaymentMethodQueryHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>();
    private readonly GetDefaultPaymentMethodQueryHandler _handler;

    public GetDefaultPaymentMethodQueryHandlerTests()
    {
        _handler = new GetDefaultPaymentMethodQueryHandler(_repository);
    }

    #region Helper Methods

    private static PaymentMethod CreateTestPaymentMethod(
        long id = 1,
        long userId = 1,
        string stripeId = "pm_test123",
        string brand = "visa",
        string last4 = "4242",
        bool isDefault = true)
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
    public async Task Handle_WithValidUserId_ShouldReturnDefault()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: true);
        var query = new GetDefaultPaymentMethodQuery("U00000001");

        _repository
            .GetDefaultByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsDefault.Should().BeTrue();
        result.Value.Brand.Should().Be("visa");
        result.Value.Last4.Should().Be("4242");
    }

    [Fact]
    public async Task Handle_WithNoDefault_ShouldReturnNull()
    {
        // Arrange
        var query = new GetDefaultPaymentMethodQuery("U00000001");

        _repository
            .GetDefaultByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectDtoProperties()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(
            id: 5,
            brand: "amex",
            last4: "9999",
            isDefault: true);
        var query = new GetDefaultPaymentMethodQuery("U00000001");

        _repository
            .GetDefaultByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.Id.Should().Be(paymentMethod.Id.ToString());
        dto.Brand.Should().Be("amex");
        dto.Last4.Should().Be("9999");
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
        var query = new GetDefaultPaymentMethodQuery("invalid-user-id");

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
        var query = new GetDefaultPaymentMethodQuery("U00000001");

        _repository
            .GetDefaultByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetDefaultByUserIdAsync(
            Arg.Is<UserId>(id => id.Value == 1),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
