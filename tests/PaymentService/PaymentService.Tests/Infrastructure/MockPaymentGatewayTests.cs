using FluentAssertions;
using PaymentService.Domain.Interfaces;
using PaymentService.Infrastructure.Gateway;
using Microsoft.Extensions.Logging;
using Moq;

namespace PaymentService.Tests.Infrastructure;

public class MockPaymentGatewayTests
{
    private readonly Mock<ILogger<MockPaymentGateway>> _loggerMock;
    private readonly MockPaymentGateway _gateway;

    public MockPaymentGatewayTests()
    {
        _loggerMock = new Mock<ILogger<MockPaymentGateway>>();
        _gateway = new MockPaymentGateway(_loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldReturnResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;

        // Act
        var result = await _gateway.ProcessPaymentAsync(orderId, amount);

        // Assert
        result.Should().NotBeNull();
        
        if (result.Success)
        {
            result.TransactionId.Should().NotBeNullOrEmpty();
            result.TransactionId.Should().StartWith("TXN-");
            result.GatewayResponse.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().BeNull();
        }
        else
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.GatewayResponse.Should().NotBeNullOrEmpty();
            result.TransactionId.Should().BeNull();
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldIncludeAmount_InResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;

        // Act
        var result = await _gateway.ProcessPaymentAsync(orderId, amount);

        // Assert
        result.GatewayResponse.Should().NotBeNullOrEmpty();
        
        if (result.Success)
        {
            result.GatewayResponse.Should().Contain(amount.ToString("C"));
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldSimulateDelay()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        var startTime = DateTime.UtcNow;

        // Act
        await _gateway.ProcessPaymentAsync(orderId, amount);
        var endTime = DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        duration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(400));
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldGenerateUniqueTransactionIds()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        var transactionIds = new HashSet<string>();

        // Act - Make multiple calls
        for (int i = 0; i < 10; i++)
        {
            var result = await _gateway.ProcessPaymentAsync(orderId, amount);
            if (result.Success && result.TransactionId != null)
            {
                transactionIds.Add(result.TransactionId);
            }
        }

        // Assert - All successful transactions should have unique IDs
        transactionIds.Should().HaveCountGreaterThan(0);
        transactionIds.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(10.00)]
    [InlineData(99.99)]
    [InlineData(1000.50)]
    [InlineData(0.01)]
    public async Task ProcessPaymentAsync_ShouldHandleVariousAmounts(decimal amount)
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var result = await _gateway.ProcessPaymentAsync(orderId, amount);

        // Assert
        result.Should().NotBeNull();
        result.GatewayResponse.Should().NotBeNullOrEmpty();
    }
}
