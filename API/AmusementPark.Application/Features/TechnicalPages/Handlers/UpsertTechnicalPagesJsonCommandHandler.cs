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

public sealed class UpsertTechnicalPagesJsonCommandHandler : ICommandHandler<UpsertTechnicalPagesJsonCommand, ApplicationResult<TechnicalPageJsonUpsertResult>>
{
    private readonly ITechnicalPageRepository repository;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public UpsertTechnicalPagesJsonCommandHandler(ITechnicalPageRepository repository, ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.repository = repository;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult<TechnicalPageJsonUpsertResult>> HandleAsync(UpsertTechnicalPagesJsonCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Pages.Count == 0)
        {
            return ApplicationResult<TechnicalPageJsonUpsertResult>.Failure(ApplicationErrors.Required(nameof(command.Pages)));
        }

        List<TechnicalPageResult> pages = new List<TechnicalPageResult>();
        int createdCount = 0;
        int updatedCount = 0;

        foreach (TechnicalPage page in command.Pages)
        {
            ApplicationResult<TechnicalPage> normalized = TechnicalPageNormalizer.NormalizeForSave(page);
            if (!normalized.IsSuccess || normalized.Value is null)
            {
                return ApplicationResult<TechnicalPageJsonUpsertResult>.Failure(normalized.Errors);
            }

            TechnicalPageUpsertOutcome outcome = await this.repository.UpsertBySlugAsync(normalized.Value, cancellationToken);
            if (outcome.Created)
            {
                createdCount++;
            }
            else
            {
                updatedCount++;
            }

            pages.Add(TechnicalPageResult.FromDomain(outcome.Page));
        }

        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<TechnicalPageJsonUpsertResult>.Success(new TechnicalPageJsonUpsertResult
        {
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            Pages = pages,
        });
    }
}

