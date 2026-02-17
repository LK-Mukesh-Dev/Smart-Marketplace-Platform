using PaymentService.Application.Events;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace PaymentService.Application.EventHandlers;

public class InventoryReservedEventHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<InventoryReservedEventHandler> _logger;

    public InventoryReservedEventHandler(
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IIdempotencyStore idempotencyStore,
        ILogger<InventoryReservedEventHandler> logger)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _idempotencyStore = idempotencyStore ?? throw new ArgumentNullException(nameof(idempotencyStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool Success, string Message, Guid? PaymentId)> HandleAsync(
        InventoryReservedEvent evt, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing InventoryReserved event for Order: {OrderId}", evt.OrderId);

        try
        {
            // Check idempotency
            if (await _idempotencyStore.ExistsAsync(evt.OrderId, cancellationToken))
            {
                var existingPaymentId = await _idempotencyStore.GetPaymentIdAsync(evt.OrderId, cancellationToken);
                _logger.LogInformation("Payment already processed for Order: {OrderId}, PaymentId: {PaymentId}", 
                    evt.OrderId, existingPaymentId);
                return (true, "Payment already processed", existingPaymentId);
            }

            // Create payment
            var payment = new Payment(evt.OrderId, evt.Amount);
            payment.MarkProcessing();

            await _paymentRepository.CreateAsync(payment, cancellationToken);

            // Process payment through gateway
            var result = await _paymentGateway.ProcessPaymentAsync(evt.OrderId, evt.Amount, cancellationToken);

            if (result.Success)
            {
                payment.MarkSuccess(result.TransactionId!, result.GatewayResponse);
                await _paymentRepository.UpdateAsync(payment, cancellationToken);
                await _idempotencyStore.SaveAsync(evt.OrderId, payment.Id, cancellationToken);

                _logger.LogInformation("Payment completed successfully. Order: {OrderId}, Transaction: {TransactionId}", 
                    evt.OrderId, result.TransactionId);

                return (true, "Payment successful", payment.Id);
            }
            else
            {
                payment.MarkFailed(result.ErrorMessage ?? "Payment gateway error");
                await _paymentRepository.UpdateAsync(payment, cancellationToken);
                await _idempotencyStore.SaveAsync(evt.OrderId, payment.Id, cancellationToken);

                _logger.LogWarning("Payment failed. Order: {OrderId}, Reason: {Reason}", 
                    evt.OrderId, result.ErrorMessage);

                return (false, result.ErrorMessage ?? "Payment failed", payment.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for Order: {OrderId}", evt.OrderId);
            return (false, $"Payment processing error: {ex.Message}", null);
        }
    }
}
