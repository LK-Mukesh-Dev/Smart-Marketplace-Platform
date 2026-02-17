using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Commands;
using UserService.Application.DTOs;
using UserService.Infrastructure.DbContext;
using Xunit;

namespace UserService.Tests.IntegrationTests;

public class UserControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public UserControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UserDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<UserDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WithValidCommand()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "integration@example.com",
            Password = "Password123",
            FirstName = "Integration",
            LastName = "Test",
            PhoneNumber = "+1234567890",
            Role = Domain.Entities.UserRole.User
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/user/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        user.Should().NotBeNull();
        user!.Email.Should().Be("integration@example.com");
        user.FirstName.Should().Be("Integration");
        user.LastName.Should().Be("Test");
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "duplicate@example.com",
            Password = "Password123",
            FirstName = "Test",
            LastName = "User"
        };

        // Register first time
        await _client.PostAsJsonAsync("/api/user/register", command);

        // Act - Try to register again
        var response = await _client.PostAsJsonAsync("/api/user/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WithValidCredentials()
    {
        // Arrange - First register a user
        var registerCommand = new RegisterUserCommand
        {
            Email = "login@example.com",
            Password = "Password123",
            FirstName = "Login",
            LastName = "Test"
        };

        await _client.PostAsJsonAsync("/api/user/register", registerCommand);

        var loginDto = new LoginDto
        {
            Email = "login@example.com",
            Password = "Password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/user/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrEmpty();
        authResponse.User.Should().NotBeNull();
        authResponse.User.Email.Should().Be("login@example.com");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WithInvalidCredentials()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/user/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_ShouldReturnUnauthorized_WithoutToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/user/profile/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
