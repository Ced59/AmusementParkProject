using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Results;

public sealed class SitemapDocumentResult
{
    public string Content { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/xml";

    public bool WasGeneratedOnDemand { get; init; }
}
