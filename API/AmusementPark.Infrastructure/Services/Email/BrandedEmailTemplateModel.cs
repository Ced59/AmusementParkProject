using System.Net;
using System.Text;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed class BrandedEmailTemplateModel
{
    public string Language { get; init; } = "en";

    public string Preheader { get; init; } = string.Empty;

    public string Badge { get; init; } = "Amusement Park";

    public string Title { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Paragraphs { get; init; } = Array.Empty<string>();

    public BrandedEmailAction? Action { get; init; }

    public IReadOnlyCollection<BrandedEmailMetric> Metrics { get; init; } = Array.Empty<BrandedEmailMetric>();

    public BrandedEmailHighlight? Highlight { get; init; }

    public string FooterNote { get; init; } = "Amusement Park";
}
