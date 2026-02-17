using FluentAssertions;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests.UnitTests.Domain;

public class UserEntityTests
{
    [Fact]
    public void User_ShouldBeCreated_WithValidProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var passwordHash = "hashed_password";
        var firstName = "John";
        var lastName = "Doe";
        var role = UserRole.User;

        // Act
        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        user.Id.Should().Be(userId);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.Role.Should().Be(UserRole.User);
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void User_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().Be(Guid.Empty);
        user.Email.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().BeNull();
        user.LastLoginAt.Should().BeNull();
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Admin)]
    public void User_ShouldAcceptValidRoles(UserRole role)
    {
        // Arrange & Act
        var user = new User { Role = role };

        // Assert
        user.Role.Should().Be(role);
    }
}
