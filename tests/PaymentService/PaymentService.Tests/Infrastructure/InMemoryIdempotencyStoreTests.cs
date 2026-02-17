using FluentAssertions;
using PaymentService.Infrastructure.Idempotency;

namespace PaymentService.Tests.Infrastructure;

public class InMemoryIdempotencyStoreTests
{
    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_ForNewOrderId()
    {
        // Arrange
        var store = new InMemoryIdempotencyStore();
        var orderId = Guid.NewGuid();

        // Act
        var exists = await store.ExistsAsync(orderId);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_AfterSaving()
    {
        // Arrange
        var store = new InMemoryIdempotencyStore();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        await store.SaveAsync(orderId, paymentId);

        // Act
        var exists = await store.ExistsAsync(orderId);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_ShouldStorePaymentId()
    {
        // Arrange
        var store = new InMemoryIdempotencyStore();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        // Act
        await store.SaveAsync(orderId, paymentId);
        var retrievedPaymentId = await store.GetPaymentIdAsync(orderId);

        // Assert
        retrievedPaymentId.Should().Be(paymentId);
    }

    [Fact]
    public async Task GetPaymentIdAsync_ShouldReturnNull_ForNonExistentOrderId()
    {
        // Arrange
        var store = new InMemoryIdempotencyStore();
        var orderId = Guid.NewGuid();

        // Act
        var paymentId = await store.GetPaymentIdAsync(orderId);

        // Assert
        paymentId.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ShouldHandleMultipleOrders()
    {
        // Arrange
        var store = new InMemoryIdempotencyStore();
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var paymentId1 = Guid.NewGuid();
        var paymentId2 = Guid.NewGuid();

        // Act
        await store.SaveAsync(orderId1, paymentId1);
        await store.SaveAsync(orderId2, paymentId2);

        // Assert
        var retrievedPaymentId1 = await store.GetPaymentIdAsync(orderId1);
        var retrievedPaymentId2 = await store.GetPaymentIdAsync(orderId2);

        retrievedPaymentId1.Should().Be(paymentId1);
        retrievedPaymentId2.Should().Be(paymentId2);
    }

    [Fact]
    public async Task SaveAsync_ShouldNotOverwriteExisting()
    {
        // Arrange
        var store = new InMemoryIdempotencyStore();
        var orderId = Guid.NewGuid();
        var paymentId1 = Guid.NewGuid();
        var paymentId2 = Guid.NewGuid();

        // Act
        await store.SaveAsync(orderId, paymentId1);
        await store.SaveAsync(orderId, paymentId2); // Should not overwrite

        // Assert
        var retrievedPaymentId = await store.GetPaymentIdAsync(orderId);
        retrievedPaymentId.Should().Be(paymentId1); // Original value should be preserved
    }

    [Fact]
    public async Task Store_ShouldBeThreadSafe()
    {
        // Arrange
        var store = new InMemoryIdempotencyStore();
        var orderId = Guid.NewGuid();
        var tasks = new List<Task>();

        // Act - Multiple concurrent saves
        for (int i = 0; i < 100; i++)
        {
            var paymentId = Guid.NewGuid();
            tasks.Add(Task.Run(async () => await store.SaveAsync(orderId, paymentId)));
        }

        await Task.WhenAll(tasks);

        // Assert - Should have exactly one entry
        var exists = await store.ExistsAsync(orderId);
        var paymentId = await store.GetPaymentIdAsync(orderId);

        exists.Should().BeTrue();
        paymentId.Should().NotBeNull();
    }
}
