using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed class ListParkGraphUpsertHistoryQueryHandler : IQueryHandler<ListParkGraphUpsertHistoryQuery, IReadOnlyCollection<ParkGraphUpsertHistoryEntry>>
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;

    private readonly IParkGraphUpsertHistoryRepository historyRepository;

    public ListParkGraphUpsertHistoryQueryHandler(IParkGraphUpsertHistoryRepository historyRepository)
    {
        this.historyRepository = historyRepository;
    }

    public async Task<IReadOnlyCollection<ParkGraphUpsertHistoryEntry>> HandleAsync(ListParkGraphUpsertHistoryQuery query, CancellationToken cancellationToken = default)
    {
        int limit = query.Limit <= 0
            ? DefaultLimit
            : Math.Min(query.Limit, MaxLimit);

        ParkGraphUpsertHistoryQuery historyQuery = new ParkGraphUpsertHistoryQuery
        {
            TargetParkId = string.IsNullOrWhiteSpace(query.TargetParkId) ? null : query.TargetParkId.Trim(),
            Limit = limit,
        };

        return await this.historyRepository.ListRecentAsync(historyQuery, cancellationToken);
    }
}
