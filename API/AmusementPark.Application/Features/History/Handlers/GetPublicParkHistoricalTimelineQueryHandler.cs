using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetPublicParkHistoricalTimelineQueryHandler :
    IQueryHandler<GetPublicParkHistoricalTimelineQuery, ApplicationResult<PublicParkHistoricalTimelineResult>>
{
    private readonly PublicParkHistoricalDataLoader dataLoader;
    private readonly IHistoricalSourceRepository historicalSourceRepository;

    public GetPublicParkHistoricalTimelineQueryHandler(
        PublicParkHistoricalDataLoader dataLoader,
        IHistoricalSourceRepository historicalSourceRepository)
    {
        this.dataLoader = dataLoader;
        this.historicalSourceRepository = historicalSourceRepository;
    }

    public async Task<ApplicationResult<PublicParkHistoricalTimelineResult>> HandleAsync(
        GetPublicParkHistoricalTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<PublicParkHistoricalTimelineResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        if (query.Page < 1
            || query.PageSize < 1
            || query.PageSize > GetPublicParkHistoricalTimelineQuery.MaximumPageSize)
        {
            return ApplicationResult<PublicParkHistoricalTimelineResult>.Failure(
                ApplicationErrors.InvalidPagination());
        }

        string parkId = query.ParkId.Trim();
        PublicParkHistoricalScope? scope = await this.dataLoader.LoadScopeAsync(parkId, cancellationToken);
        if (scope is null)
        {
            return ApplicationResult<PublicParkHistoricalTimelineResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), parkId));
        }

        PagedResult<HistoricalFact> factPage = await this.dataLoader.GetTimelinePageAsync(
            scope,
            query.Page,
            query.PageSize,
            cancellationToken);
        HistoricalFact[] pageFacts = factPage.Items.ToArray();
        HistoricalSourceRevisionReference[] sourceReferences = pageFacts
            .SelectMany(static fact => fact.SourceReferences)
            .Distinct()
            .ToArray();
        IReadOnlyCollection<HistoricalSourceReference> loadedSources = sourceReferences.Length == 0
            ? Array.Empty<HistoricalSourceReference>()
            : await this.historicalSourceRepository.GetRevisionsAsync(
                sourceReferences,
                cancellationToken);
        Dictionary<(Guid Id, int Revision), HistoricalSourceReference> publicSources = loadedSources
            .Where(static source => source.PublicationState == HistoricalPublicationState.Published
                && source.Accessibility != HistoricalSourceAccessibility.Withdrawn)
            .ToDictionary(static source => (source.Id, source.Revision));
        PublicHistoricalTimelineEntryResult[] entries = pageFacts
            .Select(fact => new PublicHistoricalTimelineEntryResult(
                fact,
                fact.SourceReferences
                    .Select(reference => publicSources.GetValueOrDefault(
                        (reference.SourceId, reference.Revision)))
                    .Where(static source => source is not null)
                    .Select(static source => source!)
                    .ToArray()))
            .ToArray();
        PagedResult<PublicHistoricalTimelineEntryResult> page = new(
            entries,
            query.Page,
            query.PageSize,
            factPage.TotalItems);

        return ApplicationResult<PublicParkHistoricalTimelineResult>.Success(
            new PublicParkHistoricalTimelineResult(scope.Park, page, scope.ZoneNames));
    }
}
