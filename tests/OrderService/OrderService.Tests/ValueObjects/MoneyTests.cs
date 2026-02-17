using FluentAssertions;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Money_ShouldBeCreated_WithValidData()
    {
        // Arrange & Act
        var money = new Money(100.50m, "USD");

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_ShouldThrowException_ForNegativeAmount()
    {
        // Act & Assert
        var act = () => new Money(-10, "USD");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Money_ShouldAdd_WithSameCurrency()
    {
        // Arrange
        var money1 = new Money(10, "USD");
        var money2 = new Money(20, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(30);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_ShouldThrowException_WhenAddingDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(10, "USD");
        var money2 = new Money(20, "EUR");

        // Act & Assert
        var act = () => money1 + money2;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Money_ShouldMultiply_ByInteger()
    {
        // Arrange
        var money = new Money(10, "USD");

        // Act
        var result = money * 3;

        // Assert
        result.Amount.Should().Be(30);
        result.Currency.Should().Be("USD");
    }
}
