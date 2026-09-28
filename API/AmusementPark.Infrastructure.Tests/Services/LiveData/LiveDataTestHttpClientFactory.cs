namespace AmusementPark.Infrastructure.Tests.Services.LiveData;

internal sealed class LiveDataTestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient httpClient;

    public LiveDataTestHttpClientFactory(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public HttpClient CreateClient(string name)
    {
        return this.httpClient;
    }
}
