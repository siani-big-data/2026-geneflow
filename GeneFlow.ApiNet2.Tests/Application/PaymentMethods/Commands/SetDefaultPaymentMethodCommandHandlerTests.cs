using GeneFlow.ApiNet2.Application.PaymentMethods.Commands.SetDefaultPaymentMethod;
using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;

namespace GeneFlow.ApiNet2.Tests.Application.PaymentMethods.Commands;

/// <summary>
/// Unit tests for SetDefaultPaymentMethodCommandHandler.
/// </summary>
public class SetDefaultPaymentMethodCommandHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>();
    private readonly IStripeService _stripeService = Substitute.For<IStripeService>();
    private readonly SetDefaultPaymentMethodCommandHandler _handler;

    public SetDefaultPaymentMethodCommandHandlerTests()
    {
        _handler = new SetDefaultPaymentMethodCommandHandler(
            _repository,
            _stripeService);
    }

    #region Helper Methods

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
    public async Task Handle_WithValidId_ShouldReturnSuccess()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { paymentMethod });

        _stripeService.IsConfigured.Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAlreadyDefault_ShouldSucceed()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: true);
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldUpdatePreviousDefault()
    {
        // Arrange
        var currentDefault = CreateTestPaymentMethod(id: 1, isDefault: true);
        var newDefault = CreateTestPaymentMethod(id: 2, isDefault: false, stripeId: "pm_new123");
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            newDefault.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(newDefault);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { currentDefault, newDefault });

        _stripeService.IsConfigured.Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).UpdateAsync(
            Arg.Is<PaymentMethod>(pm => pm.Id == currentDefault.Id && !pm.IsDefault),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUpdateStripe_WhenConfigured()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { paymentMethod });

        _stripeService.IsConfigured.Returns(true);
        _stripeService
            .GetOrCreateCustomerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("cus_test123");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _stripeService.Received(1).SetDefaultPaymentMethodAsync(
            "cus_test123",
            paymentMethod.StripePaymentMethodId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotUpdateStripe_WhenNotConfigured()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { paymentMethod });

        _stripeService.IsConfigured.Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _stripeService.DidNotReceive().SetDefaultPaymentMethodAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidId_ShouldReturnError()
    {
        // Arrange
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            "invalid-payment-method-id");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentMethod_ShouldReturnNotFound()
    {
        // Arrange
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            "M00000001");

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethod?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenNotOwner_ShouldReturnNotFound()
    {
        // Arrange - Payment method belongs to different user
        var paymentMethod = CreateTestPaymentMethod(userId: 2); // Belongs to user 2
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001", // User 1 trying to set default
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Repository Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryUpdate()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { paymentMethod });

        _stripeService.IsConfigured.Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).UpdateAsync(
            Arg.Is<PaymentMethod>(pm => pm.IsDefault),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { paymentMethod });

        _stripeService.IsConfigured.Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyDefault_ShouldNotCallSaveChanges()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: true);
        var command = new SetDefaultPaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
