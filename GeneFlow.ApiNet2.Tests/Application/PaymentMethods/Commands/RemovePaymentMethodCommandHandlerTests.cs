using GeneFlow.ApiNet2.Application.PaymentMethods.Commands.RemovePaymentMethod;
using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;

namespace GeneFlow.ApiNet2.Tests.Application.PaymentMethods.Commands;

/// <summary>
/// Unit tests for RemovePaymentMethodCommandHandler.
/// </summary>
public class RemovePaymentMethodCommandHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>();
    private readonly IStripeService _stripeService = Substitute.For<IStripeService>();
    private readonly RemovePaymentMethodCommandHandler _handler;

    public RemovePaymentMethodCommandHandlerTests()
    {
        _handler = new RemovePaymentMethodCommandHandler(
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
        var command = new RemovePaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _stripeService.IsConfigured.Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOnlyPaymentMethod_ShouldAllowRemoval()
    {
        // Arrange - Single default payment method (only one, so it can be removed)
        var paymentMethod = CreateTestPaymentMethod(isDefault: true);
        var command = new RemovePaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { paymentMethod }); // Only one method

        _stripeService.IsConfigured.Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallStripeDetach_WhenConfigured()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new RemovePaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _stripeService.IsConfigured.Returns(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _stripeService.Received(1).DetachPaymentMethodAsync(
            paymentMethod.StripePaymentMethodId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotCallStripeDetach_WhenNotConfigured()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new RemovePaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _stripeService.IsConfigured.Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _stripeService.DidNotReceive().DetachPaymentMethodAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidId_ShouldReturnError()
    {
        // Arrange
        var command = new RemovePaymentMethodCommand(
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
        var command = new RemovePaymentMethodCommand(
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
    public async Task Handle_WhenDefaultMethod_AndOtherMethodsExist_ShouldReturnError()
    {
        // Arrange - Default payment method with other methods existing
        var defaultMethod = CreateTestPaymentMethod(id: 1, isDefault: true);
        var otherMethod = CreateTestPaymentMethod(id: 2, isDefault: false);
        var command = new RemovePaymentMethodCommand(
            "U00000001",
            defaultMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(defaultMethod);

        _repository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentMethod> { defaultMethod, otherMethod }); // Multiple methods

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotRemoveDefault");
    }

    [Fact]
    public async Task Handle_WhenNotOwner_ShouldReturnNotFound()
    {
        // Arrange - Payment method belongs to different user
        var paymentMethod = CreateTestPaymentMethod(userId: 2); // Belongs to user 2
        var command = new RemovePaymentMethodCommand(
            "U00000001", // User 1 trying to remove
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
    public async Task Handle_ShouldCallRepositoryRemove()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new RemovePaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _stripeService.IsConfigured.Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).RemoveAsync(
            paymentMethod,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var paymentMethod = CreateTestPaymentMethod(isDefault: false);
        var command = new RemovePaymentMethodCommand(
            "U00000001",
            paymentMethod.Id.ToString());

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(paymentMethod);

        _stripeService.IsConfigured.Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
