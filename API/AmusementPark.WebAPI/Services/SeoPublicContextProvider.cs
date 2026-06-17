using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.WebAPI.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AmusementPark.WebAPI.Services;

public sealed class SeoPublicContextProvider : IPublicSeoContextProvider
{
    private readonly SeoSettings settings;
    private readonly IWebHostEnvironment environment;

    public SeoPublicContextProvider(IOptions<SeoSettings> settings, IWebHostEnvironment environment)
    {
        this.settings = settings.Value;
        this.environment = environment;
    }

    public Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken)
    {
        PublicSeoContext context = new PublicSeoContext(
            this.settings.GetNormalizedPublicBaseUrl(requireHttps: !this.environment.IsDevelopment()),
            this.settings.SupportedLanguages);

        return Task.FromResult(context);
    }
}
