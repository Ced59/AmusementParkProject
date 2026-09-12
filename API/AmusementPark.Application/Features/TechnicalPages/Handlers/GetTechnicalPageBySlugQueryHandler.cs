using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.TechnicalPages.Commands;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.TechnicalPages.Queries;
using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.Application.Features.TechnicalPages.Services;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.TechnicalPages.Handlers;

public sealed class GetTechnicalPageBySlugQueryHandler : IQueryHandler<GetTechnicalPageBySlugQuery, ApplicationResult<TechnicalPageResult>>
{
    private readonly ITechnicalPageRepository repository;

    public GetTechnicalPageBySlugQueryHandler(ITechnicalPageRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<TechnicalPageResult>> HandleAsync(GetTechnicalPageBySlugQuery query, CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalPage> normalized = TechnicalPageNormalizer.NormalizeForSave(new TechnicalPage
        {
            CategoryKey = "lookup",
            CategoryNames = RequiredLocalizedPlaceholder("lookup"),
            Slug = query.Slug,
            Titles = RequiredLocalizedPlaceholder("lookup"),
            Summaries = RequiredLocalizedPlaceholder("lookup"),
        });
        string slug = normalized.Value?.Slug ?? string.Empty;
        if (string.IsNullOrWhiteSpace(slug))
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.NotFound("technical-page.not-found", "Technical page not found."));
        }

        TechnicalPage? page = await this.repository.GetBySlugAsync(slug, query.IncludeHidden, cancellationToken);
        if (page is null)
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.NotFound("technical-page.not-found", "Technical page not found."));
        }

        return ApplicationResult<TechnicalPageResult>.Success(TechnicalPageResult.FromDomain(page));
    }

    private static List<LocalizedText> RequiredLocalizedPlaceholder(string value)
    {
        return new List<LocalizedText>
        {
            new LocalizedText("fr", value),
            new LocalizedText("en", value),
            new LocalizedText("de", value),
            new LocalizedText("nl", value),
            new LocalizedText("it", value),
            new LocalizedText("es", value),
            new LocalizedText("pl", value),
            new LocalizedText("pt", value),
        };
    }
}

