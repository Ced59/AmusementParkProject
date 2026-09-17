using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Services.Email;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Email;

public sealed class NotificationDigestEmailSenderTests
{
    [Fact]
    public async Task SendAsync_ShouldProduceBrandedAccessibleMultipartContentAndOneClickHeaders()
    {
        EmailMessage? captured = null;
        Mock<IEmailSender> transport = new Mock<IEmailSender>(MockBehavior.Strict);
        transport.Setup(sender => sender.SendAsync(
                It.IsAny<EmailMessage>(),
                CancellationToken.None))
            .Callback<EmailMessage, CancellationToken>((message, _) => captured = message)
            .Returns(Task.CompletedTask);
        Mock<IUserAuthenticationSettings> settings =
            new Mock<IUserAuthenticationSettings>(MockBehavior.Strict);
        settings.SetupGet(value => value.FrontendBaseUrl)
            .Returns("https://amusement-parks.fun");
        NotificationDigestEmailSender sender = new NotificationDigestEmailSender(
            transport.Object,
            settings.Object,
            new BrandedEmailTemplateRenderer());
        NotificationDigestEmailMessage message = new NotificationDigestEmailMessage(
            "visitor@example.com",
            "fr",
            NotificationFrequency.WeeklyDigest,
            new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc),
            new[]
            {
                new NotificationDigestEmailEntry(
                    "Taron",
                    "Phantasialand",
                    FactualEventType.OpenedConfirmed,
                    FactualChangeStatus.Published,
                    "Site officiel",
                    new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc)),
            },
            1,
            "signed-token");

        await sender.SendAsync(message, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("visitor@example.com", captured.To);
        Assert.Contains("Ton résumé de la semaine", captured.Subject);
        Assert.Contains("<html lang=\"fr\">", captured.HtmlBody);
        Assert.Contains("Phantasialand", captured.HtmlBody);
        Assert.Contains("Taron", captured.HtmlBody);
        Assert.Contains("notifications", captured.HtmlBody);
        Assert.Contains("Phantasialand · Taron", captured.TextBody);
        Assert.Contains("Gérer mes notifications", captured.TextBody);
        Assert.Contains("https://amusement-parks.fun/fr/profile/notifications", captured.TextBody);
        Assert.DoesNotContain("signed-token", captured.HtmlBody);
        Assert.DoesNotContain("signed-token", captured.TextBody);
        Assert.NotNull(captured.Headers);
        Assert.Equal(
            "<https://amusement-parks.fun/api/notification-email/unsubscribe?token=signed-token>",
            captured.Headers["List-Unsubscribe"]);
        Assert.Equal("List-Unsubscribe=One-Click", captured.Headers["List-Unsubscribe-Post"]);
        transport.VerifyAll();
        settings.VerifyAll();
    }
}
