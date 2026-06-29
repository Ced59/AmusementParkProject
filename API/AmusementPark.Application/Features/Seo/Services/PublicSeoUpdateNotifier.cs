using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class PublicSeoUpdateNotifier : IPublicSeoUpdateNotifier
{
    private readonly PublicSeoUrlResolver urlResolver;
    private readonly IPublicSeoContextProvider contextProvider;
    private readonly ISeoSitemapSettingsRepository settingsRepository;
    private readonly IIndexNowSubmitter indexNowSubmitter;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public PublicSeoUpdateNotifier(
        PublicSeoUrlResolver urlResolver,
        IPublicSeoContextProvider contextProvider,
        ISeoSitemapSettingsRepository settingsRepository,
        IIndexNowSubmitter indexNowSubmitter,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.urlResolver = urlResolver;
        this.contextProvider = contextProvider;
        this.settingsRepository = settingsRepository;
        this.indexNowSubmitter = indexNowSubmitter;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task NotifyAsync(PublicSeoUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);

        try
        {
            PublicSeoContext context = await this.contextProvider.GetAsync(cancellationToken);
            IReadOnlyCollection<string> relativeUrls = await this.urlResolver.ResolveAsync(update, context.SupportedLanguages, cancellationToken);
            if (relativeUrls.Count == 0)
            {
                return;
            }

            if (!update.SuppressSitemapRefresh)
            {
                await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
            }

            SeoSitemapSettings settings = await this.settingsRepository.GetAsync(cancellationToken);
            if (!settings.IsIndexNowEnabled)
            {
                return;
            }

            string publicBaseUrl = SitemapXmlWriter.NormalizePublicBaseUrl(context.PublicBaseUrl);
            List<string> absoluteUrls = relativeUrls
                .Select(url => $"{publicBaseUrl}{NormalizeRelativePath(url)}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            await this.indexNowSubmitter.SubmitAsync(settings, publicBaseUrl, absoluteUrls, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // SEO side effects must not turn a committed write into an application failure.
        }
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        string value = relativePath.Trim();
        return value.StartsWith('/') ? value : $"/{value}";
    }
}

public sealed class NoOpSeoSitemapRefreshScheduler : ISeoSitemapRefreshScheduler
{
    public Task RequestRefreshAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
