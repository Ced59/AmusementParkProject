using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

/// <summary>
/// Soumet des URLs à IndexNow ou à un endpoint compatible.
/// </summary>
public interface IIndexNowSubmitter
{
    Task<IndexNowSubmissionResult> SubmitAsync(SeoSitemapSettings settings, string publicBaseUrl, IReadOnlyCollection<string> absoluteUrls, CancellationToken cancellationToken);
}
