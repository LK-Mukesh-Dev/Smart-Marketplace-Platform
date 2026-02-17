using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class NotificationRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public EventType EventType { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string Recipient { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public string? Payload { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? IdempotencyKey { get; private set; }

    private NotificationRequest() { }

    public NotificationRequest(
        Guid userId,
        EventType eventType,
        NotificationChannel channel,
        string recipient,
        string subject,
        string body,
        string? payload = null,
        int maxRetries = 3)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient cannot be empty", nameof(recipient));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body cannot be empty", nameof(body));

        Id = Guid.NewGuid();
        UserId = userId;
        EventType = eventType;
        Channel = channel;
        Recipient = recipient;
        Subject = subject;
        Body = body;
        Payload = payload;
        Status = NotificationStatus.Pending;
        RetryCount = 0;
        MaxRetries = maxRetries;
        CreatedAt = DateTime.UtcNow;
        
        // Generate idempotency key
        IdempotencyKey = GenerateIdempotencyKey(userId, eventType, channel);
    }

    private static string GenerateIdempotencyKey(Guid userId, EventType eventType, NotificationChannel channel)
    {
        return $"{userId}:{eventType}:{channel}";
    }

    public void MarkAsSent()
    {
        if (Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Notification already marked as sent");

        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        if (Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Cannot mark sent notification as failed");

        Status = NotificationStatus.Failed;
        FailedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void IncrementRetryCount()
    {
        if (Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Cannot retry sent notification");

        if (RetryCount >= MaxRetries)
            throw new InvalidOperationException($"Max retries ({MaxRetries}) exceeded");

        RetryCount++;
        Status = NotificationStatus.Retrying;
    }

    public bool CanRetry()
    {
        return Status != NotificationStatus.Sent && RetryCount < MaxRetries;
    }

    public bool IsExpired(TimeSpan expirationTime)
    {
        return DateTime.UtcNow - CreatedAt > expirationTime;
    }
}
