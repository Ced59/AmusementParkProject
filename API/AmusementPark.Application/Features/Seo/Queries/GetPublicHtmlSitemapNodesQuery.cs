using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Queries;

public sealed record GetPublicHtmlSitemapNodesQuery(
    string Language,
    string? ParentNodeId,
    IReadOnlyCollection<string> SupportedLanguages,
    bool IncludeDescendants = false)
    : IQuery<ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>>;
