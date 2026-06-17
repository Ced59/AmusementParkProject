using System.Net;
using System.Text;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Configuration.Videos;
using AmusementPark.Infrastructure.Services.Videos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Videos;

public sealed class ExternalVideoMetadataProviderTests
{
    [Theory]
    [InlineData("https://youtube.com/watch?v=WlLikBWTmbg&is=lUPBiTdjFOXks1sk", "WlLikBWTmbg")]
    [InlineData("https://www.youtube.com/watch?si=lUPBiTdjFOXks1sk&v=WlLikBWTmbg&feature=shared", "WlLikBWTmbg")]
    [InlineData("https://m.youtube.com/watch?v=WlLikBWTmbg", "WlLikBWTmbg")]
    [InlineData("https://music.youtube.com/watch?v=WlLikBWTmbg", "WlLikBWTmbg")]
    [InlineData("https://youtu.be/WlLikBWTmbg?si=lUPBiTdjFOXks1sk", "WlLikBWTmbg")]
    [InlineData("https://www.youtube.com/embed/WlLikBWTmbg?start=12", "WlLikBWTmbg")]
    [InlineData("https://www.youtube-nocookie.com/embed/WlLikBWTmbg?rel=0", "WlLikBWTmbg")]
    [InlineData("https://www.youtube.com/shorts/WlLikBWTmbg?feature=share", "WlLikBWTmbg")]
    [InlineData("https://www.youtube.com/live/WlLikBWTmbg?si=lUPBiTdjFOXks1sk", "WlLikBWTmbg")]
    [InlineData("https://www.youtube.com/v/WlLikBWTmbg?version=3", "WlLikBWTmbg")]
    [InlineData("https://www.youtube.com/e/WlLikBWTmbg", "WlLikBWTmbg")]
    [InlineData("https://www.youtube.com/attribution_link?u=%2Fwatch%3Fv%3DWlLikBWTmbg%26feature%3Dshare", "WlLikBWTmbg")]
    public async Task ResolveAsync_WhenYouTubeUrlUsesSupportedVideoFormat_ShouldKeepOnlyVideoIdInCanonicalUrl(string url, string expectedVideoId)
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler("{}");
        ExternalVideoMetadataProvider provider = new ExternalVideoMetadataProvider(
            new SingleClientFactory(new HttpClient(handler)),
            new VideoMetadataSettings
            {
                YouTubeApiKey = string.Empty,
                YouTubeOEmbedBaseUrl = string.Empty,
            },
            NullLogger<ExternalVideoMetadataProvider>.Instance);

        AmusementPark.Application.Features.Videos.Contracts.ResolvedVideoMetadata? metadata =
            await provider.ResolveAsync(url, CancellationToken.None);

        Assert.NotNull(metadata);
        Assert.Equal(VideoHostingProvider.YouTube, metadata!.HostingProvider);
        Assert.Equal(expectedVideoId, metadata.ExternalId);
        Assert.Equal($"https://www.youtube.com/watch?v={expectedVideoId}", metadata.CanonicalUrl);
        Assert.Equal($"https://www.youtube.com/embed/{expectedVideoId}", metadata.EmbedUrl);
        Assert.Equal("url", metadata.MetadataSource);
    }

    [Fact]
    public async Task ResolveAsync_WhenYouTubeApiKeyIsConfigured_ShouldReadMetadataAndViewCountWithoutComments()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler("""
            {
              "items": [
                {
                  "snippet": {
                    "publishedAt": "2024-01-02T03:04:05Z",
                    "channelId": "UC123",
                    "title": "Onride test",
                    "description": "Stable description",
                    "defaultAudioLanguage": "fr-FR",
                    "channelTitle": "Creator name",
                    "thumbnails": {
                      "default": { "url": "https://i.ytimg.com/vi/abcdefghijk/default.jpg", "width": 120, "height": 90 },
                      "maxres": { "url": "https://i.ytimg.com/vi/abcdefghijk/maxresdefault.jpg", "width": 1280, "height": 720 }
                    }
                  },
                  "contentDetails": {
                    "duration": "PT2M3S"
                  },
                  "statistics": {
                    "viewCount": "123456"
                  }
                }
              ]
            }
            """);

        ExternalVideoMetadataProvider provider = new ExternalVideoMetadataProvider(
            new SingleClientFactory(new HttpClient(handler)),
            new VideoMetadataSettings
            {
                YouTubeApiKey = "test-key",
            },
            NullLogger<ExternalVideoMetadataProvider>.Instance);

        AmusementPark.Application.Features.Videos.Contracts.ResolvedVideoMetadata? metadata =
            await provider.ResolveAsync("https://youtu.be/abcdefghijk", CancellationToken.None);

        Assert.NotNull(metadata);
        Assert.Equal(VideoHostingProvider.YouTube, metadata!.HostingProvider);
        Assert.Equal("https://www.youtube.com/watch?v=abcdefghijk", metadata.CanonicalUrl);
        Assert.Equal("https://www.youtube.com/embed/abcdefghijk", metadata.EmbedUrl);
        Assert.Equal("Onride test", metadata.Title);
        Assert.Equal("Creator name", metadata.CreatorName);
        Assert.Equal("https://i.ytimg.com/vi/abcdefghijk/maxresdefault.jpg", metadata.ThumbnailUrl);
        Assert.Equal("fr", metadata.DetectedLanguageCode);
        Assert.Equal(TimeSpan.FromSeconds(123), metadata.Duration);
        Assert.Equal(123456L, metadata.ViewCount);
        Assert.Equal("youtube-data-api", metadata.MetadataSource);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("part=snippet,contentDetails,statistics", handler.LastRequestUri!.Query);
        Assert.DoesNotContain("comment", handler.LastRequestUri.Query, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public SingleClientFactory(HttpClient client)
        {
            this.client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return this.client;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string responseBody;

        public RecordingHttpMessageHandler(string responseBody)
        {
            this.responseBody = responseBody;
        }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequestUri = request.RequestUri;
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(this.responseBody, Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }
    }
}
