using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Email;
using AmusementPark.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Email;

public sealed class ParkWeatherNotificationEmailServiceTests
{
    [Fact]
    public async Task NotifyRunStartedAsync_WhenRunIsManual_ShouldNotSend()
    {
        Mock<IEmailSender> emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        ParkWeatherNotificationEmailService service = CreateService(emailSender.Object);

        await service.NotifyRunStartedAsync(new ParkWeatherRun
        {
            Trigger = ParkWeatherRunTrigger.Manual,
            Status = ParkWeatherRunStatus.Running,
        }, CancellationToken.None);

        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NotifyRunCompletedAsync_WhenAutomaticRunCompleted_ShouldSendResultToAdmin()
    {
        string? to = null;
        string? subject = null;
        string? htmlBody = null;
        Mock<IEmailSender> emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((capturedTo, capturedSubject, capturedBody, _) =>
            {
                to = capturedTo;
                subject = capturedSubject;
                htmlBody = capturedBody;
            })
            .Returns(Task.CompletedTask);
        ParkWeatherNotificationEmailService service = CreateService(emailSender.Object);

        await service.NotifyRunCompletedAsync(new ParkWeatherRun
        {
            Id = "run-1",
            Trigger = ParkWeatherRunTrigger.Automatic,
            Status = ParkWeatherRunStatus.CompletedWithFailures,
            TotalParkCount = 12,
            SucceededParkCount = 10,
            FailedParkCount = 2,
            WarningParkCount = 1,
            CompletedAtUtc = new DateTime(2026, 6, 20, 3, 0, 0, DateTimeKind.Utc),
            Message = "Weather refresh completed with failures.",
        }, CancellationToken.None);

        Assert.Equal("admin@amusement-parks.fun", to);
        Assert.Equal("Resultat batch meteo - Amusement Park", subject);
        Assert.NotNull(htmlBody);
        Assert.Contains("CompletedWithFailures", htmlBody);
        Assert.Contains("12", htmlBody);
        Assert.Contains("10", htmlBody);
        Assert.Contains("2", htmlBody);
        emailSender.VerifyAll();
    }

    private static ParkWeatherNotificationEmailService CreateService(IEmailSender emailSender)
    {
        return new ParkWeatherNotificationEmailService(
            emailSender,
            new EmailNotificationSettings
            {
                AdminAddress = "admin@amusement-parks.fun",
            },
            new BrandedEmailTemplateRenderer(),
            new Mock<ILogger<ParkWeatherNotificationEmailService>>().Object);
    }
}
