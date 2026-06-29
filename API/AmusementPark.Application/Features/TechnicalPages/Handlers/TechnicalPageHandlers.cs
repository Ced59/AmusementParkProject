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

public sealed class GetTechnicalPageLinkIndexQueryHandler : IQueryHandler<GetTechnicalPageLinkIndexQuery, ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>>
{
    private readonly ITechnicalPageRepository repository;

    public GetTechnicalPageLinkIndexQueryHandler(ITechnicalPageRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>> HandleAsync(GetTechnicalPageLinkIndexQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TechnicalPage> pages = await this.repository.GetPublicLinkIndexAsync(cancellationToken);
        IReadOnlyCollection<TechnicalPageResult> results = pages.Select(TechnicalPageResult.FromDomain).ToList();
        return ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>.Success(results);
    }
}

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

public sealed class UpsertTechnicalPagesJsonCommandHandler : ICommandHandler<UpsertTechnicalPagesJsonCommand, ApplicationResult<TechnicalPageJsonUpsertResult>>
{
    private readonly ITechnicalPageRepository repository;

    public UpsertTechnicalPagesJsonCommandHandler(ITechnicalPageRepository repository)
    {
        this.repository = repository;
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

        return ApplicationResult<TechnicalPageJsonUpsertResult>.Success(new TechnicalPageJsonUpsertResult
        {
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            Pages = pages,
        });
    }
}
