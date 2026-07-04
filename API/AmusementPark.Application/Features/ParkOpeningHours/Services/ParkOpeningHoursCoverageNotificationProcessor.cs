using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursCoverageNotificationProcessor
{
    private const int ExpiredThresholdDays = 0;

    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly IParkRepository parkRepository;
    private readonly ParkOpeningHoursAdminStatusResolver statusResolver;
    private readonly IParkOpeningHoursNotificationService notificationService;

    public ParkOpeningHoursCoverageNotificationProcessor(
        IParkOpeningHoursRepository openingHoursRepository,
        IParkRepository parkRepository,
        ParkOpeningHoursAdminStatusResolver statusResolver,
        IParkOpeningHoursNotificationService notificationService)
    {
        this.openingHoursRepository = openingHoursRepository;
        this.parkRepository = parkRepository;
        this.statusResolver = statusResolver;
        this.notificationService = notificationService;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await this.ProcessAsync(DateTime.UtcNow, cancellationToken);
    }

    internal async Task ProcessAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkOpeningHoursScheduleSummary> summaries = await this.openingHoursRepository.GetConfiguredSummariesAsync(cancellationToken);
        List<ParkOpeningHoursCoverageNotificationCandidate> candidates = summaries
            .SelectMany(summary => this.ResolveCandidates(summary, utcNow))
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        List<string> parkIds = candidates
            .Select(static candidate => candidate.Summary.ParkId)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        Dictionary<string, Park> parksById = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToDictionary(static park => park.Id!, StringComparer.Ordinal);

        foreach (ParkOpeningHoursCoverageNotificationCandidate candidate in candidates)
        {
            if (!parksById.TryGetValue(candidate.Summary.ParkId, out Park? park))
            {
                continue;
            }

            bool marked = await this.openingHoursRepository.TryMarkCoverageNotificationSentAsync(
                candidate.Summary.ParkId,
                candidate.ThresholdDays,
                candidate.LocalDate,
                cancellationToken);
            if (!marked)
            {
                continue;
            }

            await this.notificationService.NotifyCoverageThresholdReachedAsync(
                new ParkOpeningHoursCoverageNotification
                {
                    ParkId = candidate.Summary.ParkId,
                    ParkName = string.IsNullOrWhiteSpace(park.Name) ? candidate.Summary.ParkId : park.Name!,
                    ThresholdDays = candidate.ThresholdDays,
                    CompleteForDays = candidate.Coverage.CompleteForDays ?? 0,
                    WarningThresholdDays = candidate.Coverage.WarningThresholdDays,
                    CompleteUntilDate = candidate.Coverage.CompleteUntilDate,
                    TimeZoneId = candidate.Summary.TimeZoneId,
                    LocalDate = candidate.LocalDate,
                },
                cancellationToken);
        }
    }

    private IReadOnlyCollection<ParkOpeningHoursCoverageNotificationCandidate> ResolveCandidates(ParkOpeningHoursScheduleSummary summary, DateTime utcNow)
    {
        if (!summary.HasScheduleData || !summary.LastDate.HasValue)
        {
            return Array.Empty<ParkOpeningHoursCoverageNotificationCandidate>();
        }

        ParkOpeningHoursAdminCoverage coverage = this.statusResolver.ResolveCoverage(summary, utcNow);
        if (!coverage.CompleteForDays.HasValue)
        {
            return Array.Empty<ParkOpeningHoursCoverageNotificationCandidate>();
        }

        DateOnly localDate = this.statusResolver.ResolveLocalDate(summary.TimeZoneId, utcNow);
        List<ParkOpeningHoursCoverageNotificationCandidate> candidates = new List<ParkOpeningHoursCoverageNotificationCandidate>();

        if (this.statusResolver.IsCoverageNotificationThresholdReached(summary, ParkOpeningHoursAdminStatusResolver.NeedsUpdateWithinDays, utcNow))
        {
            candidates.Add(new ParkOpeningHoursCoverageNotificationCandidate(summary, coverage, ParkOpeningHoursAdminStatusResolver.NeedsUpdateWithinDays, localDate));
        }

        if (this.statusResolver.IsCoverageNotificationThresholdReached(summary, ExpiredThresholdDays, utcNow))
        {
            candidates.Add(new ParkOpeningHoursCoverageNotificationCandidate(summary, coverage, ExpiredThresholdDays, localDate));
        }

        return candidates;
    }

    private sealed record ParkOpeningHoursCoverageNotificationCandidate(
        ParkOpeningHoursScheduleSummary Summary,
        ParkOpeningHoursAdminCoverage Coverage,
        int ThresholdDays,
        DateOnly LocalDate);
}
