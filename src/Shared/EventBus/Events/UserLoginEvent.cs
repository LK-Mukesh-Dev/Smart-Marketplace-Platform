namespace EventBus.Events;

public class UserLoginEvent : Abstractions.IntegrationEvent
{
    public Guid UserId { get; set; }
    public DateTime LoginTime { get; set; }
}
