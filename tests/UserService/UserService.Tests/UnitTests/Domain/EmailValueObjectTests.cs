using FluentAssertions;
using UserService.Domain.ValueObjects;
using Xunit;

namespace UserService.Tests.UnitTests.Domain;

public class EmailValueObjectTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@example.com")]
    public void Email_ShouldBeCreated_WithValidEmail(string validEmail)
    {
        // Act
        var email = new Email(validEmail);

        // Assert
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Theory]
    [InlineData("TEST@EXAMPLE.COM", "test@example.com")]
    [InlineData("User@Domain.COM", "user@domain.com")]
    public void Email_ShouldBeLowerCase(string input, string expected)
    {
        // Act
        var email = new Email(input);

        // Assert
        email.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Email_ShouldThrowException_WhenEmpty(string invalidEmail)
    {
        // Act
        Action act = () => new Email(invalidEmail);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be empty");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user name@example.com")]
    public void Email_ShouldThrowException_WithInvalidFormat(string invalidEmail)
    {
        // Act
        Action act = () => new Email(invalidEmail);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid email format");
    }

    [Fact]
    public void Email_ShouldImplicitlyConvertToString()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act
        string emailString = email;

        // Assert
        emailString.Should().Be("test@example.com");
    }
}
