using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.ParkOpeningHours.Models;

namespace AmusementPark.Application.Features.ParkOpeningHours.Ports;

public interface IParkOpeningHoursRepository
{
    Task<ParkOpeningHoursSchedule?> GetByParkIdAsync(string parkId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkOpeningHoursSchedule>> GetByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkOpeningHoursSchedule>> GetPublicTextByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary>> GetSummariesByParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkOpeningHoursScheduleSummary>> GetConfiguredSummariesAsync(CancellationToken cancellationToken);

    Task<bool> TryMarkCoverageNotificationSentAsync(string parkId, int thresholdDays, DateOnly localDate, CancellationToken cancellationToken);

    Task<ParkOpeningHoursSchedule> UpsertAsync(ParkOpeningHoursSchedule schedule, CancellationToken cancellationToken);

    Task<ParkOpeningHoursFactualWriteResult> UpsertWithFactualChangeAsync(
        ParkOpeningHoursSchedule schedule,
        ParkOpeningHoursFactualChangeDraft? factualChange,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkOpeningHoursPendingFactualChange>> GetPendingFactualChangesAsync(
        int maximumCount,
        CancellationToken cancellationToken);

    Task<bool> MarkFactualChangeRecordedAsync(
        string parkId,
        string outboxEntryId,
        CancellationToken cancellationToken);
}
