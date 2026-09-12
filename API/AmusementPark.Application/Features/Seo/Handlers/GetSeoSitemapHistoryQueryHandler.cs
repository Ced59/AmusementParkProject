using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Commands;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.Application.Features.Seo.Services;

namespace AmusementPark.Application.Features.Seo.Handlers;

public sealed class GetSeoSitemapHistoryQueryHandler : IQueryHandler<GetSeoSitemapHistoryQuery, ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>>
{
    private readonly ISeoSitemapGenerationHistoryRepository historyRepository;

    public GetSeoSitemapHistoryQueryHandler(ISeoSitemapGenerationHistoryRepository historyRepository)
    {
        this.historyRepository = historyRepository;
    }

    public async Task<ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>> HandleAsync(GetSeoSitemapHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.PageSize <= 0 || query.PageSize > 100)
        {
            return ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>.Failure(ApplicationErrors.InvalidPagination());
        }

        PagedResult<SitemapGenerationHistoryEntry> page = await this.historyRepository.SearchAsync(new PagedQuery(query.Page, query.PageSize), cancellationToken);
        return ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>.Success(page);
    }
}
