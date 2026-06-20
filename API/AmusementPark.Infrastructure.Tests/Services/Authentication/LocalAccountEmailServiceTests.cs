using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Services.Authentication;
using AmusementPark.Infrastructure.Services.Email;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Authentication;

public sealed class LocalAccountEmailServiceTests
{
    [Fact]
    public async Task SendEmailConfirmationAsync_ShouldSendBrandedHtmlWithConfirmationUrl()
    {
        string? htmlBody = null;
        Mock<IEmailSender> emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(sender => sender.SendAsync("user@example.com", "Confirme ton compte Amusement Park", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => htmlBody = body)
            .Returns(Task.CompletedTask);
        LocalAccountEmailService service = new LocalAccountEmailService(
            emailSender.Object,
            new TestUserAuthenticationSettings(),
            new BrandedEmailTemplateRenderer());

        await service.SendEmailConfirmationAsync(new User
        {
            Email = "user@example.com",
            PreferredLanguage = "fr",
        }, "token value", CancellationToken.None);

        Assert.NotNull(htmlBody);
        Assert.Contains("AMUSEMENT-PARKS", htmlBody);
        Assert.Contains("https://amusement-parks.fun/fr/confirm-account?token=token%20value", htmlBody);
        Assert.Contains("Confirmer mon compte", htmlBody);
        emailSender.VerifyAll();
    }

    private sealed class TestUserAuthenticationSettings : IUserAuthenticationSettings
    {
        public int EmailConfirmationTokenExpirationHours => 24;

        public int PasswordResetTokenExpirationMinutes => 60;

        public int TokenRefreshLimitMinutes => 45;

        public string FrontendBaseUrl => "https://amusement-parks.fun";
    }
}
