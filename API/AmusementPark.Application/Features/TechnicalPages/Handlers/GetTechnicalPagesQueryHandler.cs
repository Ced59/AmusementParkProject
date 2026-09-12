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

public sealed class GetTechnicalPagesQueryHandler : IQueryHandler<GetTechnicalPagesQuery, ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>>
{
    private readonly ITechnicalPageRepository repository;

    public GetTechnicalPagesQueryHandler(ITechnicalPageRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>> HandleAsync(GetTechnicalPagesQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TechnicalPage> pages = await this.repository.GetAllAsync(query.IncludeHidden, cancellationToken);
        IReadOnlyCollection<TechnicalPageResult> results = pages.Select(TechnicalPageResult.FromDomain).ToList();
        return ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>.Success(results);
    }
}

