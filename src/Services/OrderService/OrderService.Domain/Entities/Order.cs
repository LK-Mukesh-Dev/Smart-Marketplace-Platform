using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; }
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public Address ShippingAddress { get; private set; }
    public Money TotalAmount { get; private set; }
    public Money ShippingCost { get; private set; }
    public Money Tax { get; private set; }
    public Money GrandTotal { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public Order(Guid userId, Address shippingAddress, List<OrderItem> items, Money shippingCost, Money tax)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Order must have at least one item", nameof(items));

        Id = Guid.NewGuid();
        OrderNumber = GenerateOrderNumber();
        UserId = userId;
        Status = OrderStatus.Pending;
        PaymentStatus = PaymentStatus.Pending;
        ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress));
        ShippingCost = shippingCost ?? Money.Zero();
        Tax = tax ?? Money.Zero();
        
        _items = items;
        CalculateTotals();
        
        CreatedAt = DateTime.UtcNow;
    }

    private void CalculateTotals()
    {
        TotalAmount = _items
            .Select(i => i.TotalPrice)
            .Aggregate(Money.Zero(), (sum, price) => sum + price);

        GrandTotal = TotalAmount + ShippingCost + Tax;
    }

    public void AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to non-pending order");

        _items.Add(item);
        CalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(Guid itemId)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot remove items from non-pending order");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            CalculateTotals();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ConfirmOrder()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Can only confirm pending orders");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartProcessing()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Can only process confirmed orders");

        Status = OrderStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Ship()
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException("Can only ship processing orders");

        Status = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Can only deliver shipped orders");

        Status = OrderStatus.Delivered;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel delivered or already cancelled orders");

        Status = OrderStatus.Cancelled;
        Notes = reason;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaymentCompleted()
    {
        PaymentStatus = PaymentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaymentFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
        Status = OrderStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
