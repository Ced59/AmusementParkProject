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

public sealed class UpdateTechnicalPageCommandHandler : ICommandHandler<UpdateTechnicalPageCommand, ApplicationResult<TechnicalPageResult>>
{
    private readonly ITechnicalPageRepository repository;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public UpdateTechnicalPageCommandHandler(ITechnicalPageRepository repository, ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.repository = repository;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult<TechnicalPageResult>> HandleAsync(UpdateTechnicalPageCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.NotFound("technical-page.not-found", "Technical page not found."));
        }

        TechnicalPage? existing = await this.repository.GetByIdAsync(command.Id.Trim(), cancellationToken);
        if (existing is null)
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.NotFound("technical-page.not-found", "Technical page not found."));
        }

        ApplicationResult<TechnicalPage> normalized = TechnicalPageNormalizer.NormalizeForSave(command.Page);
        if (!normalized.IsSuccess || normalized.Value is null)
        {
            return ApplicationResult<TechnicalPageResult>.Failure(normalized.Errors);
        }

        TechnicalPage? existingWithSlug = await this.repository.GetBySlugAsync(normalized.Value.Slug, true, cancellationToken);
        if (existingWithSlug is not null && !string.Equals(existingWithSlug.Id, command.Id.Trim(), StringComparison.Ordinal))
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.Conflict("technical-page.slug.exists", "Technical page slug already exists."));
        }

        normalized.Value.CreatedAtUtc = existing.CreatedAtUtc;
        TechnicalPage? updated = await this.repository.UpdateAsync(command.Id.Trim(), normalized.Value, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<TechnicalPageResult>.Failure(ApplicationError.NotFound("technical-page.not-found", "Technical page not found."));
        }

        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<TechnicalPageResult>.Success(TechnicalPageResult.FromDomain(updated));
    }
}

