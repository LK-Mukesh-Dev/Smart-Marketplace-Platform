using FluentAssertions;
using NotificationService.Infrastructure.SmsProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace NotificationService.Tests.Infrastructure;

public class MockSmsProviderTests
{
    private readonly Mock<ILogger<MockSmsProvider>> _loggerMock;
    private readonly MockSmsProvider _provider;

    public MockSmsProviderTests()
    {
        _loggerMock = new Mock<ILogger<MockSmsProvider>>();
        _provider = new MockSmsProvider(_loggerMock.Object);
    }

    [Fact]
    public void ProviderName_ShouldReturnCorrectName()
    {
        // Act
        var name = _provider.ProviderName;

        // Assert
        name.Should().Be("Mock SMS Provider");
    }

    [Fact]
    public async Task SendSmsAsync_ShouldReturnResult()
    {
        // Arrange
        var phoneNumber = "+919876543210";
        var message = "Test message";

        // Act
        var result = await _provider.SendSmsAsync(phoneNumber, message);

        // Assert
        result.Should().NotBeNull();
        result.Cost.Should().Be(0.0m);
        
        if (result.Success)
        {
            result.MessageId.Should().NotBeNullOrEmpty();
            result.MessageId.Should().StartWith("MOCK-");
            result.ProviderResponse.Should().Contain(phoneNumber);
        }
        else
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task SendSmsAsync_ShouldSimulateDelay()
    {
        // Arrange
        var phoneNumber = "+919876543210";
        var message = "Test message";
        var startTime = DateTime.UtcNow;

        // Act
        await _provider.SendSmsAsync(phoneNumber, message);
        var endTime = DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        duration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public async Task SendSmsAsync_ShouldGenerateUniqueMessageIds()
    {
        // Arrange
        var phoneNumber = "+919876543210";
        var message = "Test message";
        var messageIds = new HashSet<string>();

        // Act - Send multiple SMS
        for (int i = 0; i < 20; i++)
        {
            var result = await _provider.SendSmsAsync(phoneNumber, message);
            if (result.Success && result.MessageId != null)
            {
                messageIds.Add(result.MessageId);
            }
        }

        // Assert - All message IDs should be unique
        messageIds.Should().HaveCountGreaterThan(0);
        messageIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SendSmsAsync_ShouldHaveHighSuccessRate()
    {
        // Arrange
        var phoneNumber = "+919876543210";
        var message = "Test message";
        var successCount = 0;
        var totalAttempts = 100;

        // Act
        for (int i = 0; i < totalAttempts; i++)
        {
            var result = await _provider.SendSmsAsync(phoneNumber, message);
            if (result.Success)
            {
                successCount++;
            }
        }

        // Assert - Should have ~95% success rate
        var successRate = (double)successCount / totalAttempts;
        successRate.Should().BeGreaterThan(0.85); // At least 85%
        successRate.Should().BeLessThan(1.0); // Less than 100% (some failures expected)
    }

    [Theory]
    [InlineData("+919876543210")]
    [InlineData("+12025551234")]
    [InlineData("9876543210")]
    public async Task SendSmsAsync_ShouldAcceptVariousPhoneFormats(string phoneNumber)
    {
        // Arrange
        var message = "Test message";

        // Act
        var result = await _provider.SendSmsAsync(phoneNumber, message);

        // Assert
        result.Should().NotBeNull();
    }
}
