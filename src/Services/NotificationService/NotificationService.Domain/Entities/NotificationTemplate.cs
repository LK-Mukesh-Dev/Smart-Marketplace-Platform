using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class NotificationTemplate
{
    public Guid Id { get; private set; }
    public EventType EventType { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Subject { get; private set; }
    public string BodyTemplate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private NotificationTemplate() { }

    public NotificationTemplate(
        EventType eventType,
        NotificationChannel channel,
        string subject,
        string bodyTemplate,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty", nameof(subject));

        if (string.IsNullOrWhiteSpace(bodyTemplate))
            throw new ArgumentException("Body template cannot be empty", nameof(bodyTemplate));

        Id = Guid.NewGuid();
        EventType = eventType;
        Channel = channel;
        Subject = subject;
        BodyTemplate = bodyTemplate;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateTemplate(string subject, string bodyTemplate)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty", nameof(subject));

        if (string.IsNullOrWhiteSpace(bodyTemplate))
            throw new ArgumentException("Body template cannot be empty", nameof(bodyTemplate));

        Subject = subject;
        BodyTemplate = bodyTemplate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public string RenderBody(Dictionary<string, string> placeholders)
    {
        var body = BodyTemplate;

        foreach (var placeholder in placeholders)
        {
            body = body.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
        }

        return body;
    }
}
