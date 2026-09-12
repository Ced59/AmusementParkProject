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

public sealed class CreateTechnicalPageCommandHandler : ICommandHandler<CreateTechnicalPageCommand, ApplicationResult<TechnicalPageResult>>
{
    private readonly ITechnicalPageRepository repository;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public CreateTechnicalPageCommandHandler(ITechnicalPageRepository repository, ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.repository = repository;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult<TechnicalPageResult>> HandleAsync(CreateTechnicalPageCommand command, CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalPage> normalized = TechnicalPageNormalizer.NormalizeForSave(command.Page);
        if (!normalized.IsSuccess || normalized.Value is null)
        {
            return ApplicationResult<TechnicalPageResult>.Failure(normalized.Errors);
        }

        TechnicalPage? existing = await this.repository.GetBySlugAsync(normalized.Value.Slug, true, cancellationToken);
        if (existing is not null)
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.Conflict("technical-page.slug.exists", "Technical page slug already exists."));
        }

        TechnicalPage created = await this.repository.CreateAsync(normalized.Value, cancellationToken);
        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<TechnicalPageResult>.Success(TechnicalPageResult.FromDomain(created));
    }
}

