using AmusementPark.Infrastructure.Services.Authentication;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Authentication;

public sealed class SmtpEmailSenderTests
{
    [Fact]
    public void BuildTextBody_WhenActionLinkContainsHref_ShouldKeepReadableUrl()
    {
        string html = """
<p>Use this secure link:</p>
<a href="https://amusement-parks.fun/fr/reset-password?token=abc%20123&amp;source=email" style="font-weight:800;">Change my password</a>
<p>This link expires soon.</p>
""";

        string textBody = SmtpEmailSender.BuildTextBody(html);

        Assert.Contains("Use this secure link:", textBody);
        Assert.Contains("Change my password", textBody);
        Assert.Contains("https://amusement-parks.fun/fr/reset-password?token=abc%20123&source=email", textBody);
        Assert.Contains("This link expires soon.", textBody);
        Assert.DoesNotContain("href=", textBody);
    }

    [Fact]
    public void BuildTextBody_WhenActionLabelContainsEncodedMarkup_ShouldNotDropTheLabel()
    {
        string html = """
<a href="https://example.test/confirm?token=value">Confirm &lt;now&gt;</a>
""";

        string textBody = SmtpEmailSender.BuildTextBody(html);

        Assert.Contains("Confirm <now>", textBody);
        Assert.Contains("https://example.test/confirm?token=value", textBody);
    }
}
