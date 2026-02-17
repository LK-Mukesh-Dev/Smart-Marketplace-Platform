using FluentAssertions;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Factories;
using NotificationService.Infrastructure.SmsProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace NotificationService.Tests.Infrastructure;

public class SmsProviderFactoryTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

    public SmsProviderFactoryTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // Setup logger factory to return mock loggers
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
    }

    [Fact]
    public void CreateProvider_ShouldReturnMockProvider_WhenConfiguredForMock()
    {
        // Arrange
        var settings = new SmsProviderSettings { Provider = "Mock" };
        var factory = new SmsProviderFactory(settings, _loggerFactoryMock.Object, _httpClientFactoryMock.Object);

        // Act
        var provider = factory.CreateProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<MockSmsProvider>();
        provider.ProviderName.Should().Be("Mock SMS Provider");
    }

    [Fact]
    public void CreateProvider_ShouldReturnTwilioProvider_WhenConfiguredForTwilio()
    {
        // Arrange
        var settings = new SmsProviderSettings
        {
            Provider = "Twilio",
            Twilio = new TwilioSettings
            {
                AccountSid = "test_sid",
                AuthToken = "test_token",
                FromPhoneNumber = "+1234567890"
            }
        };

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("Twilio"))
            .Returns(new HttpClient());

        var factory = new SmsProviderFactory(settings, _loggerFactoryMock.Object, _httpClientFactoryMock.Object);

        // Act
        var provider = factory.CreateProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<TwilioSmsProvider>();
        provider.ProviderName.Should().Be("Twilio SMS");
    }

    [Fact]
    public void CreateProvider_ShouldReturnFast2SmsProvider_WhenConfiguredForFast2SMS()
    {
        // Arrange
        var settings = new SmsProviderSettings
        {
            Provider = "Fast2SMS",
            Fast2SMS = new Fast2SmsSettings
            {
                ApiKey = "test_api_key",
                SenderId = "FSTSMS"
            }
        };

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("Fast2SMS"))
            .Returns(new HttpClient());

        var factory = new SmsProviderFactory(settings, _loggerFactoryMock.Object, _httpClientFactoryMock.Object);

        // Act
        var provider = factory.CreateProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<Fast2SmsProvider>();
        provider.ProviderName.Should().Be("Fast2SMS (India)");
    }

    [Fact]
    public void CreateProvider_ShouldReturnMSG91Provider_WhenConfiguredForMSG91()
    {
        // Arrange
        var settings = new SmsProviderSettings
        {
            Provider = "MSG91",
            MSG91 = new MSG91Settings
            {
                AuthKey = "test_auth_key",
                SenderId = "MSGIND"
            }
        };

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("MSG91"))
            .Returns(new HttpClient());

        var factory = new SmsProviderFactory(settings, _loggerFactoryMock.Object, _httpClientFactoryMock.Object);

        // Act
        var provider = factory.CreateProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<MSG91SmsProvider>();
        provider.ProviderName.Should().Be("MSG91 (India)");
    }

    [Fact]
    public void CreateProvider_ShouldThrowException_WhenProviderIsUnknown()
    {
        // Arrange
        var settings = new SmsProviderSettings { Provider = "UnknownProvider" };
        var factory = new SmsProviderFactory(settings, _loggerFactoryMock.Object, _httpClientFactoryMock.Object);

        // Act
        var act = () => factory.CreateProvider();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown SMS provider*");
    }

    [Fact]
    public void CreateProvider_ShouldThrowException_WhenTwilioSettingsMissing()
    {
        // Arrange
        var settings = new SmsProviderSettings
        {
            Provider = "Twilio",
            Twilio = null
        };
        var factory = new SmsProviderFactory(settings, _loggerFactoryMock.Object, _httpClientFactoryMock.Object);

        // Act
        var act = () => factory.CreateProvider();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Twilio settings are required*");
    }

    [Theory]
    [InlineData("mock")]
    [InlineData("MOCK")]
    [InlineData("Mock")]
    public void CreateProvider_ShouldBeCaseInsensitive(string providerName)
    {
        // Arrange
        var settings = new SmsProviderSettings { Provider = providerName };
        var factory = new SmsProviderFactory(settings, _loggerFactoryMock.Object, _httpClientFactoryMock.Object);

        // Act
        var provider = factory.CreateProvider();

        // Assert
        provider.Should().BeOfType<MockSmsProvider>();
    }
}
