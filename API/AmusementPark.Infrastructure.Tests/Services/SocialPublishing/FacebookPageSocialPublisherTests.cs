using System.Net;
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
            CreateSettings());

        SocialPublisherResult result = await publisher.PublishLinkAsync(
            new SocialPublisherRequest("Bilingual message", "https://amusement-parks.fun/fr/park/park-1/test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("123_456", result.ExternalPostId);
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
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            new StubHttpClientFactory(handler),
            CreateSettings());

        SocialPublisherOperationResult result = await publisher.RefreshLinkPreviewAsync(
            "https://amusement-parks.fun/fr/park/park-1/test",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
    public async Task DeletePostAsync_WhenPostNoLongerExists_ShouldReturnMissingResult()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "{\"error\":{\"message\":\"Unsupported get request\",\"code\":100,\"error_subcode\":33}}");
        FacebookPageSocialPublisher publisher = new FacebookPageSocialPublisher(
            new StubHttpClientFactory(handler),
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

        public HttpClient CreateClient(string name)
        {
            return this.client;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly string responseBody;

        public RecordingHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            this.statusCode = statusCode;
            this.responseBody = responseBody;
        }

        public string? RequestUri { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        public HttpMethod? Method { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
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
