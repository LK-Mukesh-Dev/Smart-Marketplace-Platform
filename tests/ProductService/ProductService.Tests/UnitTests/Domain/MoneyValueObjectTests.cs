using FluentAssertions;
using ProductService.Domain.ValueObjects;
using Xunit;

namespace ProductService.Tests.UnitTests.Domain;

public class MoneyValueObjectTests
{
    [Fact]
    public void Money_ShouldBeCreated_WithValidAmount()
    {
        // Arrange & Act
        var money = new Money(100.50m, "USD");

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_ShouldThrowException_WithNegativeAmount()
    {
        // Act
        Action act = () => new Money(-10, "USD");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void Money_ShouldThrowException_WithEmptyCurrency()
    {
        // Act
        Action act = () => new Money(100, "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Currency*");
    }

    [Fact]
    public void Money_ShouldAdd_WithSameCurrency()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_ShouldThrowException_WhenAddingDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");

        // Act
        Action act = () => { var _ = money1 + money2; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void Money_ShouldSubtract_WithSameCurrency()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(30, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(70);
    }

    [Fact]
    public void Money_ShouldMultiply_ByScalar()
    {
        // Arrange
        var money = new Money(50, "USD");

        // Act
        var result = money * 2;

        // Assert
        result.Amount.Should().Be(100);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_ShouldCompare_Correctly()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act & Assert
        (money1 > money2).Should().BeTrue();
        (money2 < money1).Should().BeTrue();
        (money1 >= money2).Should().BeTrue();
        (money2 <= money1).Should().BeTrue();
    }

    [Fact]
    public void Money_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = new Money(1234.56m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("1,234.56 USD");
    }
}
