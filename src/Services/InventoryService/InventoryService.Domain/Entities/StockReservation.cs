using InventoryService.Domain.Enums;

namespace InventoryService.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid OrderId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? Reason { get; private set; }

    private StockReservation() { }

    public StockReservation(Guid productId, Guid orderId, int quantity, int expirationMinutes = 30)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        Id = Guid.NewGuid();
        ProductId = productId;
        OrderId = orderId;
        Quantity = quantity;
        Status = ReservationStatus.Reserved;
        ReservedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt && Status == ReservationStatus.Reserved;

    public void Confirm()
    {
        if (Status != ReservationStatus.Reserved)
            throw new InvalidOperationException($"Cannot confirm reservation in {Status} status");

        Status = ReservationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void Release(string reason)
    {
        if (Status == ReservationStatus.Confirmed)
            throw new InvalidOperationException("Cannot release confirmed reservation");

        Status = ReservationStatus.Released;
        ReleasedAt = DateTime.UtcNow;
        Reason = reason;
    }

    public void MarkExpired()
    {
        if (Status != ReservationStatus.Reserved)
            throw new InvalidOperationException($"Cannot expire reservation in {Status} status");

        Status = ReservationStatus.Expired;
        ReleasedAt = DateTime.UtcNow;
        Reason = "Reservation expired";
    }
}
