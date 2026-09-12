using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Results;

namespace AmusementPark.Application.Features.Seo.Queries;

public sealed record GetPublicSitemapDocumentQuery(string? SectionKey, string PublicBaseUrl, IReadOnlyCollection<string> SupportedLanguages) : IQuery<ApplicationResult<SitemapDocumentResult>>;
