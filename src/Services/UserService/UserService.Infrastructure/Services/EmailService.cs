namespace UserService.Infrastructure.Services;

public class EmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await Task.CompletedTask;
    }
}
