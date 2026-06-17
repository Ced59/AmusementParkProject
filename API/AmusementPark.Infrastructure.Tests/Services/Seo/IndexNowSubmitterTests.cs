using System.Net;
using System.Text.Json;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Infrastructure.Services.Seo;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Seo;

public sealed class IndexNowSubmitterTests
{
    [Fact]
    public async Task SubmitAsync_WhenMoreThanTenThousandUrlsAreProvided_ShouldSendSuccessiveBatches()
    {
        RecordingHttpClientFactory httpClientFactory = new RecordingHttpClientFactory();
        IndexNowSubmitter submitter = new IndexNowSubmitter(httpClientFactory);
        SeoSitemapSettings settings = new SeoSitemapSettings
        {
            IsIndexNowEnabled = true,
            IndexNowKey = "key",
            IndexNowEndpoints = new[] { "https://api.indexnow.org/indexnow" },
        };
        IReadOnlyCollection<string> urls = Enumerable.Range(1, 10001)
            .Select(static index => $"https://example.com/fr/page-{index}")
            .ToList();

        IndexNowSubmissionResult result = await submitter.SubmitAsync(settings, "https://example.com", urls, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10001, result.SubmittedUrlCount);
        Assert.Equal(2, httpClientFactory.RequestBodies.Count);
        Assert.Equal(10000, CountUrls(httpClientFactory.RequestBodies[0]));
        Assert.Equal(1, CountUrls(httpClientFactory.RequestBodies[1]));
        Assert.Equal(new[] { "https://api.indexnow.org/indexnow" }, result.AcceptedEndpoints);
    }

    private static int CountUrls(string requestBody)
    {
        using JsonDocument document = JsonDocument.Parse(requestBody);
        JsonElement urlList = document.RootElement.GetProperty("urlList");
        return urlList.GetArrayLength();
    }

    private sealed class RecordingHttpClientFactory : IHttpClientFactory
    {
        public List<string> RequestBodies { get; } = new List<string>();

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new RecordingHttpMessageHandler(this.RequestBodies));
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<string> requestBodies;

        public RecordingHttpMessageHandler(List<string> requestBodies)
        {
            this.requestBodies = requestBodies;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string requestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            this.requestBodies.Add(requestBody);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
