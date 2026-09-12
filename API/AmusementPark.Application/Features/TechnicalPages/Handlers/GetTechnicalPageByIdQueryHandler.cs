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

public sealed class GetTechnicalPageByIdQueryHandler : IQueryHandler<GetTechnicalPageByIdQuery, ApplicationResult<TechnicalPageResult>>
{
    private readonly ITechnicalPageRepository repository;

    public GetTechnicalPageByIdQueryHandler(ITechnicalPageRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<TechnicalPageResult>> HandleAsync(GetTechnicalPageByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Id))
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.NotFound("technical-page.not-found", "Technical page not found."));
        }

        TechnicalPage? page = await this.repository.GetByIdAsync(query.Id.Trim(), cancellationToken);
        if (page is null)
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.NotFound("technical-page.not-found", "Technical page not found."));
        }

        return ApplicationResult<TechnicalPageResult>.Success(TechnicalPageResult.FromDomain(page));
    }
}

