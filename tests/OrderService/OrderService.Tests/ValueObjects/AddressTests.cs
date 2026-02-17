using FluentAssertions;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Address_ShouldBeCreated_WithValidData()
    {
        // Arrange & Act
        var address = new Address(
            "123 Main St",
            "New York",
            "NY",
            "USA",
            "10001");

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("New York");
        address.State.Should().Be("NY");
        address.Country.Should().Be("USA");
        address.PostalCode.Should().Be("10001");
    }

    [Fact]
    public void Address_ShouldThrowException_WithEmptyStreet()
    {
        // Arrange & Act
        var act = () => new Address(
            "",
            "New York",
            "NY",
            "USA",
            "10001");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Street*");
    }

    [Fact]
    public void Address_ShouldThrowException_WithEmptyCity()
    {
        // Arrange & Act
        var act = () => new Address(
            "123 Main St",
            "",
            "NY",
            "USA",
            "10001");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*City*");
    }

    [Fact]
    public void Address_ShouldThrowException_WithEmptyCountry()
    {
        // Arrange & Act
        var act = () => new Address(
            "123 Main St",
            "New York",
            "NY",
            "",
            "10001");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Country*");
    }

    [Fact]
    public void Address_ShouldThrowException_WithEmptyPostalCode()
    {
        // Arrange & Act
        var act = () => new Address(
            "123 Main St",
            "New York",
            "NY",
            "USA",
            "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*PostalCode*");
    }

    [Fact]
    public void Address_Equality_ShouldWork_WithSameValues()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "NY", "USA", "10001");
        var address2 = new Address("123 Main St", "New York", "NY", "USA", "10001");

        // Assert
        address1.Should().Be(address2);
    }

    [Fact]
    public void Address_Equality_ShouldFail_WithDifferentValues()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "NY", "USA", "10001");
        var address2 = new Address("456 Oak Ave", "Los Angeles", "CA", "USA", "90001");

        // Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Address_ToString_ShouldReturnFormattedAddress()
    {
        // Arrange
        var address = new Address(
            "123 Main St",
            "New York",
            "NY",
            "USA",
            "10001");

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Contain("123 Main St");
        result.Should().Contain("New York");
        result.Should().Contain("NY");
        result.Should().Contain("USA");
        result.Should().Contain("10001");
    }
}
