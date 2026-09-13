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
    public async Task RenderConcurrencyGate_WhenCallerStopsWaiting_ShouldRetainItsPermitUntilRenderingEnds()
    {
        ShareSocialImageRenderConcurrencyGate gate = new ShareSocialImageRenderConcurrencyGate();
        TaskCompletionSource release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource twoRendersStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int activeRenders = 0;
        int maximumActiveRenders = 0;
        Func<Task<int>> operation = async () =>
        {
            int active = Interlocked.Increment(ref activeRenders);
            UpdateMaximum(ref maximumActiveRenders, active);
            if (active == 2)
            {
                twoRendersStarted.TrySetResult();
            }

            await release.Task;
            Interlocked.Decrement(ref activeRenders);
            return active;
        };
        Task<int> first = gate.RunAsync(operation, CancellationToken.None);
        Task<int> second = gate.RunAsync(operation, CancellationToken.None);
        await twoRendersStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        using CancellationTokenSource callerCancellation = new CancellationTokenSource();
        callerCancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => first.WaitAsync(callerCancellation.Token));
        Task<int> third = gate.RunAsync(operation, CancellationToken.None);
        await Task.Delay(50);

        Assert.Equal(2, Volatile.Read(ref activeRenders));
        Assert.Equal(2, Volatile.Read(ref maximumActiveRenders));
        Assert.False(third.IsCompleted);
        release.SetResult();
        await Task.WhenAll(first, second, third);
        Assert.Equal(2, maximumActiveRenders);
    }

    [Fact]
    public async Task RenderConcurrencyGate_WhenQueuedCallerDisconnects_ShouldCancelItsPendingRender()
    {
        ShareSocialImageRenderConcurrencyGate gate = new ShareSocialImageRenderConcurrencyGate();
        TaskCompletionSource release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource twoRendersStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int activeRenders = 0;
        Func<Task<int>> operation = async () =>
        {
            int active = Interlocked.Increment(ref activeRenders);
            if (active == 2)
            {
                twoRendersStarted.TrySetResult();
            }

            await release.Task;
            Interlocked.Decrement(ref activeRenders);
            return active;
        };
        Task<int> first = gate.RunAsync(operation, CancellationToken.None);
        Task<int> second = gate.RunAsync(operation, CancellationToken.None);
        await twoRendersStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        using CancellationTokenSource queuedCancellation = new CancellationTokenSource();
        Task<int> abandoned = gate.RunAsync(operation, queuedCancellation.Token);

        queuedCancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned);
        Assert.Equal(2, Volatile.Read(ref activeRenders));
        Task<int> replacement = gate.RunAsync(operation, CancellationToken.None);
        release.SetResult();
        await Task.WhenAll(first, second, replacement);
        Assert.Equal(0, Volatile.Read(ref activeRenders));
    }

    [Fact]
    public async Task RenderWork_WhenOneOfSeveralWaitersDisconnects_ShouldKeepTheSharedRenderQueued()
    {
        ShareSocialImageRenderConcurrencyGate gate = new ShareSocialImageRenderConcurrencyGate();
        TaskCompletionSource releaseActiveRenders = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource activeRendersStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int activeRenders = 0;
        Func<Task<ShareSocialImageRenderResult>> blockingOperation = async () =>
        {
            if (Interlocked.Increment(ref activeRenders) == 2)
            {
                activeRendersStarted.TrySetResult();
            }

            await releaseActiveRenders.Task;
            return CreateRenderResult();
        };
        Task<ShareSocialImageRenderResult> firstActive = gate.RunAsync(
            blockingOperation,
            CancellationToken.None);
        Task<ShareSocialImageRenderResult> secondActive = gate.RunAsync(
            blockingOperation,
            CancellationToken.None);
        await activeRendersStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        ShareSocialImageRenderWork work = new ShareSocialImageRenderWork(
            gate,
            () => Task.FromResult(CreateRenderResult()));
        using CancellationTokenSource disconnectedCaller = new CancellationTokenSource();
        Assert.True(work.TryAttachWaiter());
        Task<ShareSocialImageRenderResult> disconnectedWaiter = work.WaitAsync(
            disconnectedCaller.Token);
        Assert.True(work.TryAttachWaiter());
        Task<ShareSocialImageRenderResult> connectedWaiter = work.WaitAsync(CancellationToken.None);

        disconnectedCaller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => disconnectedWaiter);

        Assert.False(connectedWaiter.IsCompleted);
        releaseActiveRenders.SetResult();
        await Task.WhenAll(firstActive, secondActive);
        ShareSocialImageRenderResult result = await connectedWaiter.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotEmpty(result.Content);
    }

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

    [Fact]
    public void EmbeddedSymbolFallback_ShouldCoverAllowedPublicNameSymbols()
    {
        FontCollection collection = new FontCollection();
        FontFamily family = collection.Add(System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Fonts",
            "noto-emoji-symbols.ttf"));
        Font font = family.CreateFont(24, FontStyle.Regular);

        bool isAvailable = font.TryGetGlyphs(
            new CodePoint(0x1F3A2),
            out IReadOnlyList<Glyph>? glyphs);

        Assert.True(isAvailable, "The embedded symbol fallback is missing the roller coaster glyph.");
        Assert.NotEmpty(glyphs ?? Array.Empty<Glyph>());
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
    public async Task Invalidate_ShouldEvictThePreviouslyRenderedShareImage()
    {
        ShareSocialImageModel model = CreateVisitModel("fr");
        ShareSocialImageModel unaffectedModel = model with { ShareId = "another-share" };
        using ShareSocialImageRenderer renderer = new ShareSocialImageRenderer();
        ShareSocialImageRenderResult initial = await renderer.RenderAsync(
            model,
            CancellationToken.None);
        ShareSocialImageRenderResult unaffected = await renderer.RenderAsync(
            unaffectedModel,
            CancellationToken.None);

        renderer.Invalidate(new[] { model.ShareId });
        ShareSocialImageRenderResult refreshed = await renderer.RenderAsync(
            model,
            CancellationToken.None);
        ShareSocialImageRenderResult stillCached = await renderer.RenderAsync(
            unaffectedModel,
            CancellationToken.None);

        Assert.NotSame(initial, refreshed);
        Assert.Same(unaffected, stillCached);
        Assert.Equal(initial.EntityTag, refreshed.EntityTag);
        Assert.Equal(initial.Content, refreshed.Content);
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
            "shared-visit",
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

    private static ShareSocialImageRenderResult CreateRenderResult()
    {
        return new ShareSocialImageRenderResult(
            new byte[] { 1 },
            "image/png",
            "alternative text",
            "\"etag\"");
    }

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int observed = Volatile.Read(ref maximum);
        while (candidate > observed)
        {
            int exchanged = Interlocked.CompareExchange(ref maximum, candidate, observed);
            if (exchanged == observed)
            {
                return;
            }

            observed = exchanged;
        }
    }
}
