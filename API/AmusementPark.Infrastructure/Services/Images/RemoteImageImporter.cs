using System.Net;
using System.Net.Http.Headers;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using DomainImage = AmusementPark.Core.Domain.Images.Image;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace AmusementPark.Infrastructure.Services.Images;

public sealed class RemoteImageImporter : IRemoteImageImporter
{
    public const string HttpClientName = "remote-image-import";

    private const int MaxRedirects = 5;
    private const long MaxImageBytes = 10 * 1024 * 1024;
    private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0 Safari/537.36";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IImageRepository imageRepository;
    private readonly IImageProcessingPipeline imageProcessingPipeline;
    private readonly IImageBinaryStorage imageBinaryStorage;
    private readonly ILogger<RemoteImageImporter> logger;

    public RemoteImageImporter(
        IHttpClientFactory httpClientFactory,
        IImageRepository imageRepository,
        IImageProcessingPipeline imageProcessingPipeline,
        IImageBinaryStorage imageBinaryStorage,
        ILogger<RemoteImageImporter> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.imageRepository = imageRepository;
        this.imageProcessingPipeline = imageProcessingPipeline;
        this.imageBinaryStorage = imageBinaryStorage;
        this.logger = logger;
    }

    public async Task<DomainImage?> ImportAsync(RemoteImageImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Uri.TryCreate(request.SourceUrl, UriKind.Absolute, out Uri? sourceUri) || !IsHttpUri(sourceUri))
        {
            return null;
        }

        try
        {
            DownloadedImage? downloadedImage = await this.DownloadAsync(sourceUri, cancellationToken);
            if (downloadedImage is null)
            {
                return null;
            }

            using (downloadedImage)
            {
                bool withWatermark = ShouldApplyWatermark(request.Category, request.WithWatermark);
                string imageId = Guid.NewGuid().ToString("N");
                string storagePath = $"{ToPathSegment(request.Category)}/{imageId}";
                FilePayload file = new FilePayload
                {
                    FileName = downloadedImage.FileName,
                    ContentType = downloadedImage.ContentType,
                    Length = downloadedImage.Content.Length,
                    Content = downloadedImage.Content,
                };

                ImageUploadRequest baseRequest = new ImageUploadRequest
                {
                    ImageId = imageId,
                    Category = request.Category,
                    File = file,
                    Description = request.Description,
                    WithWatermark = withWatermark,
                    OwnerType = request.OwnerType,
                    OwnerId = string.IsNullOrWhiteSpace(request.OwnerId) ? null : request.OwnerId.Trim(),
                    StoragePath = storagePath,
                    SourceUrl = request.SourceUrl.Trim(),
                };

                ImageProcessingMetadata? metadata = await this.imageProcessingPipeline.ExtractMetadataAsync(baseRequest, cancellationToken);
                if (file.Content.CanSeek)
                {
                    file.Content.Position = 0;
                }

                await this.imageBinaryStorage.SaveAsync(storagePath, file, withWatermark, cancellationToken);
                ImageUploadRequest preparedRequest = new ImageUploadRequest
                {
                    ImageId = imageId,
                    Category = request.Category,
                    File = file,
                    Description = request.Description,
                    WithWatermark = withWatermark,
                    OwnerType = request.OwnerType,
                    OwnerId = string.IsNullOrWhiteSpace(request.OwnerId) ? null : request.OwnerId.Trim(),
                    StoragePath = storagePath,
                    SourceUrl = request.SourceUrl.Trim(),
                    Width = metadata?.Width ?? 0,
                    Height = metadata?.Height ?? 0,
                    SizeInBytes = metadata?.SizeInBytes ?? file.Length,
                    GeoLocation = metadata?.GeoLocation,
                    ExifMetadata = metadata?.ExifMetadata,
                };

                return await this.imageRepository.CreateAsync(preparedRequest, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "Remote image import failed for source {SourceUrl}.", request.SourceUrl);
            return null;
        }
    }

    private async Task<DownloadedImage?> DownloadAsync(Uri sourceUri, CancellationToken cancellationToken)
    {
        HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
        Uri currentUri = sourceUri;

        for (int redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            if (!await IsAllowedExternalUriAsync(currentUri, cancellationToken))
            {
                this.logger.LogWarning("Remote image import rejected non-public URL {SourceUrl}.", currentUri);
                return null;
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            ApplyBrowserLikeHeaders(request, currentUri);

            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (IsRedirect(response.StatusCode))
            {
                Uri? nextUri = ResolveRedirectUri(currentUri, response.Headers.Location);
                if (nextUri is null || redirectCount == MaxRedirects)
                {
                    return null;
                }

                currentUri = nextUri;
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && (contentLength.Value <= 0 || contentLength.Value > MaxImageBytes))
            {
                return null;
            }

            string? mediaType = response.Content.Headers.ContentType?.MediaType;
            string fileName = BuildFileName(currentUri, mediaType, response.Content.Headers.ContentDisposition);
            MemoryStream content = new MemoryStream();
            try
            {
                await using Stream remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await CopyToMemoryWithLimitAsync(remoteStream, content, MaxImageBytes, cancellationToken);
                if (content.Length <= 0)
                {
                    content.Dispose();
                    return null;
                }

                content.Position = 0;
                IImageFormat? detectedFormat = await ImageSharpImage.DetectFormatAsync(content, cancellationToken);
                if (detectedFormat is null)
                {
                    content.Dispose();
                    return null;
                }

                content.Position = 0;
                string contentType = ResolveContentType(mediaType, detectedFormat);
                fileName = EnsureFileNameExtension(fileName, detectedFormat, contentType);
                return new DownloadedImage(content, fileName, contentType);
            }
            catch
            {
                content.Dispose();
                throw;
            }
        }

        return null;
    }

    internal static bool ShouldApplyWatermark(ImageCategory category, bool requestedWithWatermark)
    {
        return requestedWithWatermark && !IsLogoCategory(category);
    }

    private static bool IsLogoCategory(ImageCategory category)
    {
        return category is ImageCategory.Logo;
    }

    private static void ApplyBrowserLikeHeaders(HttpRequestMessage request, Uri currentUri)
    {
        request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
        request.Headers.Accept.ParseAdd("image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
        request.Headers.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        request.Headers.Referrer = new Uri(currentUri.GetLeftPart(UriPartial.Authority));
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "image");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "no-cors");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
    }

    private static async Task CopyToMemoryWithLimitAsync(Stream input, MemoryStream output, long maxBytes, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[81920];
        long totalBytes = 0;
        while (true)
        {
            int bytesRead = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (bytesRead == 0)
            {
                return;
            }

            totalBytes += bytesRead;
            if (totalBytes > maxBytes)
            {
                throw new InvalidOperationException("Remote image is larger than the configured import limit.");
            }

            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        int numericStatusCode = (int)statusCode;
        return numericStatusCode is >= 300 and <= 399;
    }

    private static Uri? ResolveRedirectUri(Uri currentUri, Uri? location)
    {
        if (location is null)
        {
            return null;
        }

        Uri nextUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
        return IsHttpUri(nextUri) ? nextUri : null;
    }

    private static bool IsHttpUri(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static async Task<bool> IsAllowedExternalUriAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!IsHttpUri(uri) || string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            return false;
        }

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        IPAddress[] addresses;
        if (IPAddress.TryParse(uri.Host, out IPAddress? literalAddress))
        {
            addresses = new[] { literalAddress };
        }
        else
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        }

        return addresses.Length > 0 && addresses.All(static address => !IsBlockedAddress(address));
    }

    private static bool IsBlockedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return IsBlockedIPv6Address(address);
        }

        IPAddress mappedAddress = address.MapToIPv4();
        byte[] bytes = mappedAddress.GetAddressBytes();
        return bytes[0] == 0 ||
               bytes[0] == 10 ||
               bytes[0] == 127 ||
               bytes[0] >= 224 ||
               (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) ||
               (bytes[0] == 169 && bytes[1] == 254) ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168) ||
               (bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19));
    }

    private static bool IsBlockedIPv6Address(IPAddress address)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal || address.Equals(IPAddress.IPv6None) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        byte[] bytes = address.GetAddressBytes();
        return (bytes[0] & 0xfe) == 0xfc;
    }

    private static string BuildFileName(Uri uri, string? mediaType, ContentDispositionHeaderValue? contentDisposition)
    {
        string? dispositionFileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName;
        string fileName = NormalizeHeaderFileName(dispositionFileName) ?? Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "remote-image";
        }

        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        string sanitized = new string(fileName.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(Path.GetExtension(sanitized)) && IsImageMediaType(mediaType)
            ? $"{sanitized}{ResolveExtension(mediaType)}"
            : sanitized;
    }

    private static string? NormalizeHeaderFileName(string? fileName)
    {
        string trimmedFileName = fileName?.Trim().Trim('"') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedFileName))
        {
            return null;
        }

        return Path.GetFileName(trimmedFileName.Replace('\\', '/'));
    }

    private static string ResolveExtension(string? mediaType)
    {
        return mediaType?.ToLowerInvariant() switch
        {
            "image/apng" => ".png",
            "image/avif" => ".avif",
            "image/bmp" => ".bmp",
            "image/gif" => ".gif",
            "image/vnd.microsoft.icon" => ".ico",
            "image/x-icon" => ".ico",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/tiff" => ".tiff",
            "image/webp" => ".webp",
            "image/x-tga" => ".tga",
            _ => ".jpg",
        };
    }

    private static string ResolveContentType(string? mediaType, IImageFormat detectedFormat)
    {
        if (IsImageMediaType(mediaType))
        {
            string detectedMimeType = detectedFormat.DefaultMimeType;
            return string.IsNullOrWhiteSpace(detectedMimeType) ? mediaType!.Trim() : detectedMimeType;
        }

        return detectedFormat.DefaultMimeType;
    }

    private static string EnsureFileNameExtension(string fileName, IImageFormat detectedFormat, string contentType)
    {
        string extension = Path.GetExtension(fileName);
        string[] detectedExtensions = detectedFormat.FileExtensions.ToArray();
        if (!string.IsNullOrWhiteSpace(extension) && detectedExtensions.Any(detectedExtension => string.Equals(extension.TrimStart('.'), detectedExtension.TrimStart('.'), StringComparison.OrdinalIgnoreCase)))
        {
            return fileName;
        }

        string resolvedExtension = detectedExtensions.FirstOrDefault()
            ?? ResolveExtension(contentType);

        return $"{Path.GetFileNameWithoutExtension(fileName)}.{resolvedExtension.TrimStart('.')}";
    }

    private static bool IsImageMediaType(string? mediaType)
    {
        return !string.IsNullOrWhiteSpace(mediaType) &&
               mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToPathSegment(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Avatar => "avatar",
            ImageCategory.Logo => "logo",
            ImageCategory.Park => "park",
            ImageCategory.ParkItem => "park_item",
            ImageCategory.Operator => "operator",
            ImageCategory.Manufacturer => "manufacturer",
            ImageCategory.Founder => "founder",
            ImageCategory.VideoThumbnail => "video_thumbnail",
            _ => "image",
        };
    }

    private sealed class DownloadedImage : IDisposable
    {
        public DownloadedImage(MemoryStream content, string fileName, string contentType)
        {
            this.Content = content;
            this.FileName = fileName;
            this.ContentType = contentType;
        }

        public MemoryStream Content { get; }

        public string FileName { get; }

        public string ContentType { get; }

        public void Dispose()
        {
            this.Content.Dispose();
        }
    }
}
