using AmusementPark.Core.Domain.FactualEvents;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.FactualEvents;

public sealed class SourceReferenceTests
{
    private static readonly DateTime PublishedAtUtc =
        new DateTime(2026, 9, 15, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldNormalizeAndRetainVerifiableSourceMetadata()
    {
        SourceReference source = new SourceReference(
            SourceReferenceType.OfficialWebsite,
            "  Example Park  ",
            "  Opening calendar  ",
            "https://example.com/calendar",
            PublishedAtUtc);

        Assert.Equal("Example Park", source.PublisherName);
        Assert.Equal("Opening calendar", source.Title);
        Assert.Equal("https://example.com/calendar", source.Url);
        Assert.Equal(PublishedAtUtc, source.PublishedAtUtc);
    }

    [Fact]
    public void Create_ShouldAcceptLongAbsoluteEvidenceUrl()
    {
        string url = $"https://example.com/{new string('a', 500)}";

        SourceReference source = CreateSource(url);

        Assert.Equal(url, source.Url);
    }

    [Fact]
    public void Create_WithUrlExpandingBeyondCanonicalLimit_ShouldRejectSource()
    {
        string url = $"https://example.com/{new string('é', 500)}";

        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => CreateSource(url));

        Assert.Equal(FactualEventErrorCodes.InvalidSource, exception.Code);
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("ftp://example.com/fact")]
    public void Create_WithUnsupportedUrl_ShouldRejectSource(string url)
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => CreateSource(url));

        Assert.Equal(FactualEventErrorCodes.InvalidSource, exception.Code);
    }

    [Fact]
    public void Create_WithNonUtcPublicationDate_ShouldRejectSource()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => new SourceReference(
                SourceReferenceType.OfficialDocument,
                "Example Park",
                "Calendar",
                "https://example.com/calendar",
                DateTime.Now));

        Assert.Equal(FactualEventErrorCodes.InvalidSource, exception.Code);
    }

    [Fact]
    public void Create_WithUnknownType_ShouldRejectSource()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => new SourceReference(
                (SourceReferenceType)99,
                "Example Park",
                "Calendar",
                "https://example.com/calendar",
                PublishedAtUtc));

        Assert.Equal(FactualEventErrorCodes.InvalidSource, exception.Code);
    }

    private static SourceReference CreateSource(string url)
    {
        return new SourceReference(
            SourceReferenceType.OfficialDocument,
            "Example Park",
            "Calendar",
            url,
            PublishedAtUtc);
    }
}
