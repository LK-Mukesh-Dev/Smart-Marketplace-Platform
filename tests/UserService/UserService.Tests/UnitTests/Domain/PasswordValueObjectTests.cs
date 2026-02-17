using FluentAssertions;
using UserService.Domain.ValueObjects;
using Xunit;

namespace UserService.Tests.UnitTests.Domain;

public class PasswordValueObjectTests
{
    [Theory]
    [InlineData("Password123")]
    [InlineData("12345678")]
    [InlineData("StrongP@ssw0rd!")]
    public void Password_ShouldBeCreated_WithValidPassword(string validPassword)
    {
        // Act
        var password = new Password(validPassword);

        // Assert
        password.Value.Should().Be(validPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Password_ShouldThrowException_WhenEmpty(string invalidPassword)
    {
        // Act
        Action act = () => new Password(invalidPassword);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be empty");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    [InlineData("Pass123")]
    public void Password_ShouldThrowException_WhenTooShort(string shortPassword)
    {
        // Act
        Action act = () => new Password(shortPassword);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password must be at least 8 characters long");
    }

    [Fact]
    public void Password_ShouldImplicitlyConvertToString()
    {
        // Arrange
        var password = new Password("Password123");

        // Act
        string passwordString = password;

        // Assert
        passwordString.Should().Be("Password123");
    }
}
