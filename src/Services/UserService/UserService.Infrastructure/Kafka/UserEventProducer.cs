namespace UserService.Infrastructure.Kafka;

public class UserEventProducer
{
    public async Task PublishUserRegisteredEventAsync(Guid userId, string email)
    {
        await Task.CompletedTask;
    }

    public async Task PublishUserLoginEventAsync(Guid userId, DateTime loginTime)
    {
        await Task.CompletedTask;
    }
}
