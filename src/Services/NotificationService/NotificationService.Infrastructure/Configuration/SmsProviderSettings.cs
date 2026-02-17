namespace NotificationService.Infrastructure.Configuration;

public class SmsProviderSettings
{
    public string Provider { get; set; } = "Mock"; // Mock, Twilio, Fast2SMS, MSG91
    public TwilioSettings? Twilio { get; set; }
    public Fast2SmsSettings? Fast2SMS { get; set; }
    public MSG91Settings? MSG91 { get; set; }
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
    public bool IsTrialAccount { get; set; } = true;
}

public class Fast2SmsSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = "FSTSMS";
    public string Route { get; set; } = "dlt"; // dlt, otp, transactional
}

public class MSG91Settings
{
    public string AuthKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = "MSGIND";
    public string Route { get; set; } = "4"; // 1=promo, 4=transactional
}
