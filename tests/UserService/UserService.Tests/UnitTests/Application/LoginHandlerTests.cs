using FluentAssertions;
using Moq;
using UserService.Application.Commands;
using UserService.Application.Handlers;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using Xunit;

namespace UserService.Tests.UnitTests.Application;

public class LoginHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtService = new Mock<IJwtService>();
        _handler = new LoginHandler(_mockRepository.Object, _mockPasswordHasher.Object, _mockJwtService.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.User,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(h => h.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _mockJwtService.Setup(j => j.GenerateToken(user))
            .Returns("jwt_token");

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("jwt_token");
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@example.com");

        _mockRepository.Verify(r => r.GetByEmailAsync(command.Email), Times.Once);
        _mockPasswordHasher.Verify(h => h.VerifyPassword(command.Password, user.PasswordHash), Times.Once);
        _mockJwtService.Verify(j => j.GenerateToken(user), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<User>(u => u.LastLoginAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");

        _mockRepository.Verify(r => r.GetByEmailAsync(command.Email), Times.Once);
        _mockPasswordHasher.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenPasswordIsInvalid()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(h => h.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");

        _mockPasswordHasher.Verify(h => h.VerifyPassword(command.Password, user.PasswordHash), Times.Once);
        _mockJwtService.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenUserIsInactive()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            IsActive = false
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(h => h.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User account is not active");

        _mockJwtService.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
