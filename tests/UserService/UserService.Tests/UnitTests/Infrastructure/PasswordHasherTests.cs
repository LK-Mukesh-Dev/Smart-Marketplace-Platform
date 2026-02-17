using FluentAssertions;
using UserService.Infrastructure.Services;
using Xunit;

namespace UserService.Tests.UnitTests.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Theory]
    [InlineData("Password123")]
    [InlineData("StrongP@ssw0rd!")]
    [InlineData("12345678")]
    public void HashPassword_ShouldReturnHash_ForValidPassword(string password)
    {
        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashes_ForSamePassword()
    {
        // Arrange
        var password = "Password123";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt generates unique salt each time
    }

    [Theory]
    [InlineData("Password123")]
    [InlineData("StrongP@ssw0rd!")]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches(string password)
    {
        // Arrange
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var correctPassword = "Password123";
        var wrongPassword = "WrongPassword";
        var hash = _passwordHasher.HashPassword(correctPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }
}
