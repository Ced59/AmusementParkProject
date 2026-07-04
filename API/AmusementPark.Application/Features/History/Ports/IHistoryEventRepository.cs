using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoryEventRepository
{
    Task<HistoryEvent?> GetByIdAsync(string eventId, bool includeHidden, CancellationToken cancellationToken);

    Task<HistoryEvent?> GetByOwnerKeyAsync(HistoryEntityType entityType, string ownerId, string key, CancellationToken cancellationToken);

    Task<PagedResult<HistoryEvent>> GetAdminPageAsync(int page, int pageSize, HistoryEntityType? entityType, string? ownerId, string? search, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoryEvent>> GetOwnerTimelineAsync(HistoryEntityType entityType, string ownerId, bool includeHidden, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoryEvent>> GetOwnerTimelinesAsync(HistoryEntityType entityType, IReadOnlyCollection<string> ownerIds, bool includeHidden, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoryEvent>> GetParkTimelineAsync(string parkId, bool includeHidden, bool includeParkItemEvents, IReadOnlyCollection<string> parkItemIds, CancellationToken cancellationToken);

    Task<bool> HasParkItemTimelineEventsAsync(string parkId, bool includeHidden, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoryEvent>> GetPublicVisibleEventsAsync(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoryEvent>> GetPublicSitemapCandidatesAsync(int limit, CancellationToken cancellationToken);

    Task<HistoryEvent> CreateAsync(HistoryEvent historyEvent, CancellationToken cancellationToken);

    Task<HistoryEvent?> UpdateAsync(string eventId, HistoryEvent historyEvent, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string eventId, CancellationToken cancellationToken);
}
