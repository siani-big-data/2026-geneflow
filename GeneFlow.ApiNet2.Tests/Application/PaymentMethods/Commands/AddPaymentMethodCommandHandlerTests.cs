using GeneFlow.ApiNet2.Application.PaymentMethods.Commands.AddPaymentMethod;
using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.PaymentMethods.Commands;

/// <summary>
/// Unit tests for AddPaymentMethodCommandHandler.
/// </summary>
public class AddPaymentMethodCommandHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>();
    private readonly IStripeService _stripeService = Substitute.For<IStripeService>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly AddPaymentMethodCommandHandler _handler;

    public AddPaymentMethodCommandHandlerTests()
    {
        _sequenceGenerator
            .NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(1L));

        _handler = new AddPaymentMethodCommandHandler(
            _repository,
            _stripeService,
            _sequenceGenerator);
    }

    #region Helper Methods

    private static StripePaymentMethodDetails CreateStripeDetails(
        string id = "pm_test123",
        string brand = "visa",
        string last4 = "4242",
        int expiryMonth = 12,
        int expiryYear = 2030)
    {
        return new StripePaymentMethodDetails(id, brand, last4, expiryMonth, expiryYear);
    }

    private static PaymentMethod CreateTestPaymentMethod(
        long id = 1,
        long userId = 1,
        string stripeId = "pm_test123",
        bool isDefault = false)
    {
        var paymentMethodId = PaymentMethodId.FromSequence(id);
        var userIdValue = new UserId(userId);
        return PaymentMethod.Create(
            paymentMethodId,
            userIdValue,
            stripeId,
            "visa",
            "4242",
            12,
            2030,
            isDefault).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_test123",
            true);

        var stripeDetails = CreateStripeDetails();

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        _stripeService
            .GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stripeDetails);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Brand.Should().Be("visa");
        result.Value.Last4.Should().Be("4242");
        result.Value.ExpiryMonth.Should().Be(12);
        result.Value.ExpiryYear.Should().Be(2030);
        result.Value.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSetAsDefault_WhenFirstPaymentMethod()
    {
        // Arrange
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_test123",
            true);

        var stripeDetails = CreateStripeDetails();

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        _stripeService
            .GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stripeDetails);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod>()); // No existing methods

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldNotSetAsDefault_WhenSetAsDefaultIsFalse()
    {
        // Arrange
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_test123",
            false); // SetAsDefault = false

        var stripeDetails = CreateStripeDetails();

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        _stripeService
            .GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stripeDetails);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldRemovePreviousDefault_WhenSettingNewDefault()
    {
        // Arrange
        var existingMethod = CreateTestPaymentMethod(id: 2, isDefault: true);
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_new123",
            true);

        var stripeDetails = CreateStripeDetails(id: "pm_new123");

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        _stripeService
            .GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stripeDetails);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { existingMethod });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).UpdateAsync(
            Arg.Is<PaymentMethod>(pm => !pm.IsDefault),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnError()
    {
        // Arrange
        var command = new AddPaymentMethodCommand(
            "invalid-user-id",
            "pm_test123",
            true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithDuplicatePaymentMethod_ShouldReturnError()
    {
        // Arrange
        var existingMethod = CreateTestPaymentMethod();
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_test123",
            true);

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingMethod); // Already exists

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExists");
    }

    [Fact]
    public async Task Handle_WithInvalidStripeToken_ShouldReturnError()
    {
        // Arrange
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_invalid",
            true);

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        _stripeService
            .GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((StripePaymentMethodDetails?)null); // Invalid token returns null

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStripeId");
    }

    #endregion

    #region Repository Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryAdd()
    {
        // Arrange
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_test123",
            true);

        var stripeDetails = CreateStripeDetails();

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        _stripeService
            .GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stripeDetails);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Any<PaymentMethod>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_test123",
            true);

        var stripeDetails = CreateStripeDetails();

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        _stripeService
            .GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stripeDetails);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallSequenceGenerator()
    {
        // Arrange
        var command = new AddPaymentMethodCommand(
            "U00000001",
            "pm_test123",
            true);

        var stripeDetails = CreateStripeDetails();

        _repository
            .GetByStripeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        _stripeService
            .GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stripeDetails);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(
            PaymentMethodId.SequenceName,
            Arg.Any<CancellationToken>());
    }

    #endregion
}
