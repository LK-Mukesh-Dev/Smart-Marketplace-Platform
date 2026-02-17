using FluentAssertions;
using Moq;
using UserService.Application.Handlers;
using UserService.Application.Queries;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using Xunit;

namespace UserService.Tests.UnitTests.Application;

public class GetUserProfileHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly GetUserProfileHandler _handler;

    public GetUserProfileHandlerTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _handler = new GetUserProfileHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUserDto_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery { UserId = userId };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.PhoneNumber.Should().Be("+1234567890");
        result.Role.Should().Be(UserRole.User);
        result.IsActive.Should().BeTrue();

        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery { UserId = userId };

        _mockRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();

        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }
}
