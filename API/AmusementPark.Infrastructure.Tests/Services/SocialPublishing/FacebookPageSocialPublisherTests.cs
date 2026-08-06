using System.Net;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;
using AmusementPark.Infrastructure.Services.SocialPublishing;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.SocialPublishing;

public sealed class FacebookPageSocialPublisherTests
{
    [Fact]
    public async Task PublishLinkAsync_WhenFacebookAcceptsPost_ShouldUsePageFeedAndBearerToken()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            "{\"id\":\"123_456\"}");
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            new StubHttpClientFactory(handler),
            new StubPublicSeoContextProvider(),
            CreateSettings());

        SocialPublisherResult result = await publisher.PublishLinkAsync(
            new SocialPublisherRequest("Bilingual message", "https://amusement-parks.fun/fr/park/park-1/test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("123_456", result.ExternalPostId);
        Assert.Equal(1, handler.PagePreparationCallCount);
        Assert.Equal(
            new[]
            {
                "https://amusement-parks.fun/fr/park/park-1/test",
                "https://graph.facebook.com/v24.0/123/feed",
            },
            handler.RequestUris);
        Assert.Equal("https://graph.facebook.com/v24.0/123/feed", handler.RequestUri);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("secret-page-token", handler.AuthorizationParameter);
        Assert.Contains("message=Bilingual+message", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("link=https%3A%2F%2Famusement-parks.fun%2Ffr%2Fpark%2Fpark-1%2Ftest", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishLinkAsync_WhenFacebookReturnsGraphError_ShouldReturnSanitizedFailure()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "{\"error\":{\"message\":\"Token expired\",\"code\":190,\"error_subcode\":463}}");
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            new StubHttpClientFactory(handler),
            new StubPublicSeoContextProvider(),
            CreateSettings());

        SocialPublisherResult result = await publisher.PublishLinkAsync(
            new SocialPublisherRequest("Message", "https://amusement-parks.fun/en/home"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("190/463", result.FailureCode);
        Assert.Equal("Token expired", result.FailureMessage);
        Assert.DoesNotContain("secret-page-token", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdatePostAsync_WhenFacebookAcceptsChange_ShouldTargetTrackedPost()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(HttpStatusCode.OK, "{\"success\":true}");
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            new StubHttpClientFactory(handler),
            new StubPublicSeoContextProvider(),
            CreateSettings());

        SocialPublisherOperationResult result = await publisher.UpdatePostAsync(
            "123_456",
            "Updated message",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://graph.facebook.com/v24.0/123_456", handler.RequestUri);
        Assert.Contains("message=Updated+message", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshLinkPreviewAsync_WhenFacebookAcceptsScrape_ShouldUseGraphRootAndBearerToken()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            "{\"url\":\"https://amusement-parks.fun/fr/park/park-1/test\"}");
        StubHttpClientFactory httpClientFactory = new StubHttpClientFactory(handler);
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            httpClientFactory,
            new StubPublicSeoContextProvider(),
            CreateSettings());

        SocialPublisherOperationResult result = await publisher.RefreshLinkPreviewAsync(
            "https://amusement-parks.fun/fr/park/park-1/test",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[]
            {
                FacebookPageSocialPublisher.PreviewPagePreparationHttpClientName,
                FacebookPageSocialPublisher.PreviewRefreshHttpClientName,
            },
            httpClientFactory.ClientNames);
        Assert.Equal(FacebookPageSocialPublisher.PreviewRefreshHttpClientName, httpClientFactory.LastClientName);
        Assert.Equal(1, handler.PagePreparationCallCount);
        Assert.True(handler.WarmupRequested);
        Assert.True(handler.WarmupRefreshRequested);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://graph.facebook.com/v24.0/", handler.RequestUri);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("secret-page-token", handler.AuthorizationParameter);
        Assert.Contains(
            "id=https%3A%2F%2Famusement-parks.fun%2Ffr%2Fpark%2Fpark-1%2Ftest",
            handler.RequestBody,
            StringComparison.Ordinal);
        Assert.Contains("scrape=true", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshLinkPreviewAsync_WhenFreshSsrHtmlIsNotSeoReady_ShouldNotAskFacebookToScrape()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            "{}",
            pageSeoReady: false);
        StubHttpClientFactory httpClientFactory = new StubHttpClientFactory(handler);
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            httpClientFactory,
            new StubPublicSeoContextProvider(),
            CreateSettings());

        SocialPublisherOperationResult result = await publisher.RefreshLinkPreviewAsync(
            "https://amusement-parks.fun/fr/park/park-1/test",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("preview-page-not-ready", result.FailureCode);
        Assert.Equal(1, handler.PagePreparationCallCount);
        Assert.Equal(0, handler.GraphCallCount);
        Assert.Equal(
            new[] { FacebookPageSocialPublisher.PreviewPagePreparationHttpClientName },
            httpClientFactory.ClientNames);
    }

    [Fact]
    public async Task PublishLinkAsync_WhenLinkTargetsAnotherSite_ShouldNotPrepareIt()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            "{\"id\":\"123_456\"}");
        StubHttpClientFactory httpClientFactory = new StubHttpClientFactory(handler);
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            httpClientFactory,
            new StubPublicSeoContextProvider(),
            CreateSettings());

        SocialPublisherResult result = await publisher.PublishLinkAsync(
            new SocialPublisherRequest("External link", "https://example.com/article"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, handler.PagePreparationCallCount);
        Assert.Equal(1, handler.GraphCallCount);
        Assert.Equal(
            new[] { FacebookPageSocialPublisher.HttpClientName },
            httpClientFactory.ClientNames);
    }

    [Fact]
    public async Task DeletePostAsync_WhenPostNoLongerExists_ShouldReturnMissingResult()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "{\"error\":{\"message\":\"Unsupported get request\",\"code\":100,\"error_subcode\":33}}");
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            new StubHttpClientFactory(handler),
            new StubPublicSeoContextProvider(),
            CreateSettings());

        SocialPublisherOperationResult result = await publisher.DeletePostAsync("123_456", CancellationToken.None);

        Assert.True(result.IsMissing);
        Assert.Equal(HttpMethod.Delete, handler.Method);
    }

    [Fact]
    public async Task GetPostAsync_WhenFacebookReturnsPost_ShouldMapSnapshot()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            "{\"id\":\"123_456\",\"message\":\"Facebook text\",\"permalink_url\":\"https://facebook.test/post\"}");
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            new StubHttpClientFactory(handler),
            new StubPublicSeoContextProvider(),
            CreateSettings());

        SocialPublisherPostSnapshotResult result = await publisher.GetPostAsync("123_456", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Exists);
        Assert.Equal("Facebook text", result.Message);
        Assert.Equal("https://facebook.test/post", result.ExternalPostUrl);
        Assert.Contains("fields=id,message,permalink_url", handler.RequestUri, StringComparison.Ordinal);
    }

    private static FacebookPagePublishingSettings CreateSettings()
    {
        return new FacebookPagePublishingSettings
        {
            Enabled = true,
            ApiVersion = "v24.0",
            PageId = "123",
            PageAccessToken = "secret-page-token",
            PageUrl = "https://www.facebook.com/amusementparksfun",
            RequestTimeoutSeconds = 10,
        };
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            this.client = new HttpClient(handler);
        }

        public string? LastClientName { get; private set; }

        public List<string> ClientNames { get; } = new List<string>();

        public HttpClient CreateClient(string name)
        {
            this.LastClientName = name;
            this.ClientNames.Add(name);
            return this.client;
        }
    }

    private sealed class StubPublicSeoContextProvider : IPublicSeoContextProvider
    {
        public Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new PublicSeoContext(
                "https://amusement-parks.fun",
                Array.Empty<string>()));
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly string responseBody;
        private readonly bool pageSeoReady;

        public RecordingHttpMessageHandler(
            HttpStatusCode statusCode,
            string responseBody,
            bool pageSeoReady = true)
        {
            this.statusCode = statusCode;
            this.responseBody = responseBody;
            this.pageSeoReady = pageSeoReady;
        }

        public List<string> RequestUris { get; } = new List<string>();

        public int PagePreparationCallCount { get; private set; }

        public int GraphCallCount { get; private set; }

        public bool WarmupRequested { get; private set; }

        public bool WarmupRefreshRequested { get; private set; }

        public string? RequestUri { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        public HttpMethod? Method { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string requestUri = request.RequestUri?.AbsoluteUri ?? string.Empty;
            this.RequestUris.Add(requestUri);
            if (request.Method == HttpMethod.Get
                && request.RequestUri?.Host == "amusement-parks.fun")
            {
                this.PagePreparationCallCount++;
                this.WarmupRequested = request.Headers.TryGetValues(
                        "X-AmusementPark-SSR-Warmup",
                        out IEnumerable<string>? warmupValues)
                    && warmupValues.Contains("1", StringComparer.Ordinal);
                this.WarmupRefreshRequested = request.Headers.TryGetValues(
                        "X-AmusementPark-SSR-Warmup-Refresh",
                        out IEnumerable<string>? refreshValues)
                    && refreshValues.Contains("1", StringComparer.Ordinal);
                HttpResponseMessage preparationResponse = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html></html>"),
                };
                preparationResponse.Headers.TryAddWithoutValidation(
                    "X-AmusementPark-Seo-Ready",
                    this.pageSeoReady ? "true" : "false");
                return preparationResponse;
            }

            this.GraphCallCount++;
            this.RequestUri = request.RequestUri?.AbsoluteUri;
            this.Method = request.Method;
            this.AuthorizationScheme = request.Headers.Authorization?.Scheme;
            this.AuthorizationParameter = request.Headers.Authorization?.Parameter;
            this.RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(this.statusCode)
            {
                Content = new StringContent(this.responseBody),
            };
        }
    }
}
