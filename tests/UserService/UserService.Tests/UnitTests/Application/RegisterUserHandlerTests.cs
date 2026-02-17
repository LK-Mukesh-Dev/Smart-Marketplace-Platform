using FluentAssertions;
using Moq;
using UserService.Application.Commands;
using UserService.Application.Handlers;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using Xunit;

namespace UserService.Tests.UnitTests.Application;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _handler = new RegisterUserHandler(_mockRepository.Object, _mockPasswordHasher.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldRegisterUser_WhenValidCommand()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "Password123",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            Role = UserRole.User
        };

        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockPasswordHasher.Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns("hashed_password");

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Role.Should().Be(UserRole.User);
        result.IsActive.Should().BeTrue();

        _mockRepository.Verify(r => r.ExistsAsync("test@example.com"), Times.Once);
        _mockPasswordHasher.Verify(h => h.HashPassword("Password123"), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "existing@example.com",
            Password = "Password123",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User with this email already exists");

        _mockRepository.Verify(r => r.ExistsAsync("existing@example.com"), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    public async Task HandleAsync_ShouldThrowException_WithInvalidEmail(string invalidEmail)
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = invalidEmail,
            Password = "Password123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task HandleAsync_ShouldThrowException_WithShortPassword(string shortPassword)
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = shortPassword,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
