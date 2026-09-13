using System.Security.Cryptography;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Services.Sharing;
using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Sharing;

public sealed class ShareSocialImageRendererTests
{
    [Fact]
    public void UnicodeFallbacks_ShouldPreferTheProductionCjkFamily()
    {
        int japanesePriority = ShareSocialImageFontFamilyComparer.GetPriority("Noto Sans CJK JP");
        int genericUnicodePriority = ShareSocialImageFontFamilyComparer.GetPriority("Noto Sans Arabic");
        int dejavuPriority = ShareSocialImageFontFamilyComparer.GetPriority("DejaVu Sans");
        int unknownPriority = ShareSocialImageFontFamilyComparer.GetPriority("Another system font");

        Assert.True(japanesePriority < genericUnicodePriority);
        Assert.True(genericUnicodePriority < dejavuPriority);
        Assert.True(dejavuPriority < unknownPriority);
        Assert.True(ShareSocialImageFontFamilyComparer.IsSupported("Noto Sans Arabic"));
        Assert.True(ShareSocialImageFontFamilyComparer.IsSupported("DejaVu Sans"));
        Assert.False(ShareSocialImageFontFamilyComparer.IsSupported("Noto Color Emoji"));
        Assert.False(ShareSocialImageFontFamilyComparer.IsSupported("Another system font"));
    }

    [Fact]
    public void EmbeddedFont_ShouldCoverEveryLocalizedLatinCharacter()
    {
        FontCollection collection = new FontCollection();
        FontFamily family = collection.Add(System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Fonts",
            "bangers-latin-complete.ttf"));
        Font font = family.CreateFont(24, FontStyle.Regular);
        const string localizedCharacters =
            "ÄÖÜäöüßÉÈÀÂÇŒéèàâçœÍÓÚÑíóúñŁĄĆĘŃÓŚŹŻłąćęńóśźżÃÕãõ";

        foreach (char character in localizedCharacters)
        {
            bool isAvailable = font.TryGetGlyphs(
                new CodePoint(character),
                out IReadOnlyList<Glyph>? glyphs);
            IReadOnlyList<Glyph> availableGlyphs = glyphs
                ?? throw new InvalidOperationException("Font API returned no glyph collection.");

            Assert.True(isAvailable, $"The embedded social-image font is missing '{character}'.");
            Assert.NotEmpty(availableGlyphs);
        }
    }

    [Fact]
    public void EmbeddedUnicodeFallback_ShouldCoverJapanesePublicNames()
    {
        FontCollection collection = new FontCollection();
        FontFamily family = collection.Add(System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Fonts",
            "noto-sans-jp-unicode.ttf"));
        Font font = family.CreateFont(24, FontStyle.Regular);

        foreach (char character in "東京")
        {
            bool isAvailable = font.TryGetGlyphs(
                new CodePoint(character),
                out IReadOnlyList<Glyph>? glyphs);

            Assert.True(isAvailable, $"The embedded Unicode fallback is missing '{character}'.");
            Assert.NotEmpty(glyphs ?? Array.Empty<Glyph>());
        }
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    [InlineData("nl")]
    [InlineData("pl")]
    [InlineData("pt")]
    public async Task RenderAsync_ShouldCreateACompleteLocalizedOpenGraphImage(string language)
    {
        ShareSocialImageModel model = CreateVisitModel(language);
        using ShareSocialImageRenderer renderer = new ShareSocialImageRenderer();

        ShareSocialImageRenderResult result = await renderer.RenderAsync(
            model,
            CancellationToken.None);
        ShareSocialImageRenderResult cached = await renderer.RenderAsync(
            model,
            CancellationToken.None);

        Assert.Same(result, cached);
        Assert.Equal("image/png", result.ContentType);
        Assert.NotEmpty(result.AlternativeText);
        Assert.DoesNotContain("park-internal-id", result.AlternativeText, StringComparison.Ordinal);
        Assert.Matches("^\"[a-f0-9]{64}\"$", result.EntityTag);
        using Image image = Image.Load(result.Content);
        Assert.Equal(ShareSocialImageTemplate.Width, image.Width);
        Assert.Equal(ShareSocialImageTemplate.Height, image.Height);
    }

    [Fact]
    public async Task RenderAsync_ShouldMatchTheApprovedVisitTemplateSnapshot()
    {
        using ShareSocialImageRenderer renderer = new ShareSocialImageRenderer();

        ShareSocialImageRenderResult result = await renderer.RenderAsync(
            CreateVisitModel("fr"),
            CancellationToken.None);
        string digest = Convert.ToHexString(SHA256.HashData(result.Content)).ToLowerInvariant();

        Assert.Equal(
            "a089020b89c5ca87db4715aa02aee3a72eedd5e114ba2348ab4e61ae49a2d7fb",
            digest);
    }

    [Fact]
    public async Task RenderAsync_WhenDayIsHiddenByThePolicy_ShouldNotReintroduceItInAlternativeText()
    {
        ShareSocialImageModel model = CreateVisitModel("fr") with
        {
            Date = new ShareSocialImageDate(2026, 7, 26, ShareDatePrecision.Month),
        };
        using ShareSocialImageRenderer renderer = new ShareSocialImageRenderer();

        ShareSocialImageRenderResult result = await renderer.RenderAsync(
            model,
            CancellationToken.None);

        Assert.Contains("juillet 2026", result.AlternativeText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("26 juillet", result.AlternativeText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("V7", result.AlternativeText, StringComparison.Ordinal);
        Assert.DoesNotContain("T1", result.AlternativeText, StringComparison.Ordinal);
    }

    private static ShareSocialImageModel CreateVisitModel(string language)
    {
        return new ShareSocialImageModel(
            SharePublicationType.VisitRecap,
            language,
            "Denain Évasion",
            new ShareSocialImageDate(2026, 7, null, ShareDatePrecision.Month),
            null,
            new[]
            {
                new ShareSocialImageMetric(ShareSocialImageMetricKind.Attractions, 8),
                new ShareSocialImageMetric(ShareSocialImageMetricKind.Rides, 14),
                new ShareSocialImageMetric(ShareSocialImageMetricKind.Rating, 4.5),
            },
            "Le Galion",
            7);
    }
}
