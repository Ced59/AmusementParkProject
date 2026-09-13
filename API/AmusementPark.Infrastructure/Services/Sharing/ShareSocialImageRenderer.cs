using System.Globalization;
using System.Security.Cryptography;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AmusementPark.Infrastructure.Services.Sharing;

public sealed class ShareSocialImageRenderer : IShareSocialImageRenderer, IDisposable
{
    private const int CacheSizeLimit = 128;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private readonly FontFamily fontFamily;
    private readonly IReadOnlyList<FontFamily> fallbackFontFamilies;
    private readonly MemoryCache cache = new MemoryCache(new MemoryCacheOptions
    {
        SizeLimit = CacheSizeLimit,
    });
    private readonly object cacheLock = new object();

    public ShareSocialImageRenderer()
    {
        FontCollection collection = new FontCollection();
        string fontPath = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Fonts",
            "bangers-latin-complete.ttf");
        this.fontFamily = collection.Add(fontPath);
        string unicodeFontPath = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Fonts",
            "noto-sans-jp-unicode.ttf");
        FontFamily unicodeFontFamily = collection.Add(unicodeFontPath);
        this.fallbackFontFamilies = new[] { unicodeFontFamily }
            .Concat(SystemFonts.Families
                .Where(static family => ShareSocialImageFontFamilyComparer.IsSupported(family.Name))
                .OrderBy(ShareSocialImageFontFamilyComparer.GetPriority)
                .ThenBy(static family => family.Name, StringComparer.Ordinal))
            .DistinctBy(static family => family.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<ShareSocialImageRenderResult> RenderAsync(
        ShareSocialImageModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        string cacheKey = ShareSocialImageCacheKeyFactory.Create(model);
        Lazy<Task<ShareSocialImageRenderResult>> rendering;
        lock (this.cacheLock)
        {
            if (!this.cache.TryGetValue(cacheKey, out rendering!))
            {
                rendering = new Lazy<Task<ShareSocialImageRenderResult>>(
                    () => this.RenderCoreAsync(model),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                this.cache.Set(
                    cacheKey,
                    rendering,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CacheDuration,
                        Size = 1,
                    });
            }
        }

        Task<ShareSocialImageRenderResult> renderingTask = rendering.Value;
        try
        {
            return await renderingTask.WaitAsync(cancellationToken);
        }
        catch
        {
            if (renderingTask.IsFaulted)
            {
                this.cache.Remove(cacheKey);
            }

            throw;
        }
    }

    public void Dispose()
    {
        this.cache.Dispose();
    }

    private async Task<ShareSocialImageRenderResult> RenderCoreAsync(ShareSocialImageModel model)
    {
        ShareSocialImageLocalizedCopy copy = ShareSocialImageCopyCatalog.Resolve(model.Language);
        CultureInfo culture = CultureInfo.GetCultureInfo(copy.Culture);
        string alternativeText = ShareSocialImageTextFormatter.BuildAlternativeText(
            model,
            copy,
            culture);

        using Image<Rgba32> image = new Image<Rgba32>(
            ShareSocialImageTemplate.Width,
            ShareSocialImageTemplate.Height,
            Color.ParseHex("090704"));
        ShareSocialImageCanvasPainter.Draw(
            image,
            model,
            copy,
            culture,
            this.fontFamily,
            this.fallbackFontFamilies);

        await using MemoryStream stream = new MemoryStream();
        await image.SaveAsPngAsync(stream, CancellationToken.None);
        byte[] content = stream.ToArray();
        string digest = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        return new ShareSocialImageRenderResult(
            content,
            "image/png",
            alternativeText,
            $"\"{digest}\"");
    }
}
