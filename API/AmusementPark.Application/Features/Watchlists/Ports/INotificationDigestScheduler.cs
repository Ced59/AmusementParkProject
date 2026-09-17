using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface INotificationDigestScheduler
{
    Task ScheduleAsync(
        FactualChangeEventId eventId,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken);
}
