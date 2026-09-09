using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Plans;

/// <summary>
/// Unit tests for the PlanPricing value object.
/// </summary>
public class PlanPricingTests
{
    #region Create - Success

    [Fact]
    public void Create_WithValidPrices_ShouldReturnSuccess()
    {
        // Act
        var result = PlanPricing.Create(29.00m, 290.00m, "EUR");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MonthlyPrice.Should().Be(29.00m);
        result.Value.AnnualPrice.Should().Be(290.00m);
        result.Value.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Create_WithZeroPrices_ShouldReturnSuccess()
    {
        // Act
        var result = PlanPricing.Create(0, 0, "USD");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFree.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldUppercaseCurrency()
    {
        // Act
        var result = PlanPricing.Create(10, 100, "eur");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("EUR");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    public void Create_WithDifferentCurrencies_ShouldSucceed(string currency)
    {
        // Act
        var result = PlanPricing.Create(10, 100, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be(currency);
    }

    #endregion

    #region Create - Failures

    [Fact]
    public void Create_WithNegativeMonthlyPrice_ShouldReturnFailure()
    {
        // Act
        var result = PlanPricing.Create(-10, 100, "EUR");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidMonthlyPrice");
    }

    [Fact]
    public void Create_WithNegativeAnnualPrice_ShouldReturnFailure()
    {
        // Act
        var result = PlanPricing.Create(10, -100, "EUR");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidAnnualPrice");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Create_WithInvalidCurrency_ShouldReturnFailure(string? currency)
    {
        // Act
        var result = PlanPricing.Create(10, 100, currency!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCurrency");
    }

    #endregion

    #region IsFree

    [Fact]
    public void IsFree_WhenBothPricesAreZero_ShouldBeTrue()
    {
        // Arrange
        var pricing = PlanPricing.Create(0, 0, "EUR").Value;

        // Assert
        pricing.IsFree.Should().BeTrue();
    }

    [Fact]
    public void IsFree_WhenMonthlyPriceIsPositive_ShouldBeFalse()
    {
        // Arrange
        var pricing = PlanPricing.Create(10, 0, "EUR").Value;

        // Assert
        pricing.IsFree.Should().BeFalse();
    }

    [Fact]
    public void IsFree_WhenAnnualPriceIsPositive_ShouldBeFalse()
    {
        // Arrange
        var pricing = PlanPricing.Create(0, 100, "EUR").Value;

        // Assert
        pricing.IsFree.Should().BeFalse();
    }

    #endregion

    #region MonthlyEquivalentFromAnnual

    [Fact]
    public void MonthlyEquivalentFromAnnual_ShouldCalculateCorrectly()
    {
        // Arrange
        var pricing = PlanPricing.Create(29, 290, "EUR").Value;

        // Act
        var monthlyEquivalent = pricing.MonthlyEquivalentFromAnnual;

        // Assert
        monthlyEquivalent.Should().BeApproximately(24.17m, 0.01m);
    }

    #endregion

    #region AnnualDiscountPercentage

    [Fact]
    public void AnnualDiscountPercentage_ShouldCalculateCorrectly()
    {
        // Arrange: 29 * 12 = 348, annual = 290, discount = (348-290)/348 * 100 = 16.67%
        var pricing = PlanPricing.Create(29, 290, "EUR").Value;

        // Act
        var discount = pricing.AnnualDiscountPercentage;

        // Assert
        discount.Should().BeApproximately(16.67m, 0.01m);
    }

    [Fact]
    public void AnnualDiscountPercentage_WhenFree_ShouldBeZero()
    {
        // Arrange
        var pricing = PlanPricing.Free();

        // Act
        var discount = pricing.AnnualDiscountPercentage;

        // Assert
        discount.Should().Be(0);
    }

    #endregion

    #region Static Factory Methods

    [Fact]
    public void Free_ShouldReturnFreePricing()
    {
        // Act
        var pricing = PlanPricing.Free();

        // Assert
        pricing.MonthlyPrice.Should().Be(0);
        pricing.AnnualPrice.Should().Be(0);
        pricing.Currency.Should().Be("EUR");
        pricing.IsFree.Should().BeTrue();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSamePricing_ShouldBeEqual()
    {
        // Arrange
        var pricing1 = PlanPricing.Create(29, 290, "EUR").Value;
        var pricing2 = PlanPricing.Create(29, 290, "EUR").Value;

        // Assert
        pricing1.Should().Be(pricing2);
    }

    [Fact]
    public void Equals_WithDifferentPricing_ShouldNotBeEqual()
    {
        // Arrange
        var pricing1 = PlanPricing.Create(29, 290, "EUR").Value;
        var pricing2 = PlanPricing.Create(39, 390, "EUR").Value;

        // Assert
        pricing1.Should().NotBe(pricing2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WhenFree_ShouldReturnFree()
    {
        // Arrange
        var pricing = PlanPricing.Free();

        // Act
        var result = pricing.ToString();

        // Assert
        result.Should().Be("Free");
    }

    [Fact]
    public void ToString_WhenPaid_ShouldReturnFormattedPrice()
    {
        // Arrange
        var pricing = PlanPricing.Create(29, 290, "EUR").Value;

        // Act
        var result = pricing.ToString();

        // Assert
        result.Should().Contain("29");
        result.Should().Contain("EUR");
    }

    #endregion
}
