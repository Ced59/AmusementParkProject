using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Results;

namespace AmusementPark.Application.Features.Seo.Queries;

public sealed record GetSeoSitemapOverviewQuery(string PublicBaseUrl) : IQuery<ApplicationResult<SeoSitemapOverviewResult>>;

public sealed record GetSeoSitemapSettingsQuery() : IQuery<ApplicationResult<SeoSitemapSettings>>;

public sealed record GetSeoSitemapHistoryQuery(int Page, int PageSize) : IQuery<ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>>;

public sealed record GetPublicSitemapDocumentQuery(string? SectionKey, string PublicBaseUrl, IReadOnlyCollection<string> SupportedLanguages, int MaxDynamicUrlsPerType) : IQuery<ApplicationResult<SitemapDocumentResult>>;
