using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Contact;
using AmusementPark.Infrastructure.Configuration.Email;
using AmusementPark.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Email;

public sealed class ContactNotificationEmailServiceTests
{
    [Fact]
    public async Task NotifySubmittedAsync_WhenEnabled_ShouldSendEscapedMessageToAdmin()
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
        ContactNotificationEmailService service = new ContactNotificationEmailService(
            emailSender.Object,
            new EmailNotificationSettings
            {
                AdminAddress = "admin@amusement-parks.fun",
                ContactAddress = "contact@amusement-parks.fun",
            },
            new BrandedEmailTemplateRenderer(),
            new Mock<ILogger<ContactNotificationEmailService>>().Object);
        ContactGrievance grievance = new ContactGrievance
        {
            Id = "grievance-1",
            Message = "Bonjour <script>alert(1)</script>",
            LanguageCode = "fr",
            IpAddress = "127.0.0.1",
            CreatedAtUtc = new DateTime(2026, 6, 20, 8, 15, 0, DateTimeKind.Utc),
        };

        await service.NotifySubmittedAsync(grievance, CancellationToken.None);

        Assert.Equal("admin@amusement-parks.fun", to);
        Assert.Equal("Nouveau message contact - Amusement Park", subject);
        Assert.NotNull(htmlBody);
        Assert.Contains("contact@amusement-parks.fun", htmlBody);
        Assert.Contains("Bonjour &lt;script&gt;alert(1)&lt;/script&gt;", htmlBody);
        Assert.DoesNotContain("Bonjour <script>alert(1)</script>", htmlBody);
        emailSender.VerifyAll();
    }

    [Fact]
    public async Task NotifySubmittedAsync_WhenDisabled_ShouldNotSend()
    {
        Mock<IEmailSender> emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        ContactNotificationEmailService service = new ContactNotificationEmailService(
            emailSender.Object,
            new EmailNotificationSettings
            {
                ContactNotificationsEnabled = false,
            },
            new BrandedEmailTemplateRenderer(),
            new Mock<ILogger<ContactNotificationEmailService>>().Object);

        await service.NotifySubmittedAsync(new ContactGrievance { Message = "Message valide" }, CancellationToken.None);

        emailSender.VerifyNoOtherCalls();
    }
}
