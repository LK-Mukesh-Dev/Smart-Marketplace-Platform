namespace NotificationService.Domain.Enums;

public enum EventType
{
    OrderPlaced = 0,
    OrderConfirmed = 1,
    OrderShipped = 2,
    OrderDelivered = 3,
    OrderCancelled = 4,
    PaymentSuccess = 5,
    PaymentFailed = 6,
    InventoryLowStock = 7,
    RefundInitiated = 8,
    RefundCompleted = 9
}
