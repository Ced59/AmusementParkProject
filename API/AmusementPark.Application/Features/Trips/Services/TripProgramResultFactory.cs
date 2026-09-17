using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripProgramResultFactory
{
    private readonly ITripParkCandidateRepository candidateRepository;
    private readonly ITripDayPlanRepository dayPlanRepository;
    private readonly IParkRepository parkRepository;

    public TripProgramResultFactory(
        ITripParkCandidateRepository candidateRepository,
        ITripDayPlanRepository dayPlanRepository,
        IParkRepository parkRepository)
    {
        this.candidateRepository = candidateRepository
            ?? throw new ArgumentNullException(nameof(candidateRepository));
        this.dayPlanRepository = dayPlanRepository
            ?? throw new ArgumentNullException(nameof(dayPlanRepository));
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
    }

    public async Task<TripProgramResult> BuildAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        Task<IReadOnlyCollection<TripParkCandidate>> candidatesTask =
            this.candidateRepository.ListAsync(tripPlanId, cancellationToken);
        Task<IReadOnlyCollection<TripDayPlan>> daysTask =
            this.dayPlanRepository.ListAsync(tripPlanId, cancellationToken);
        await Task.WhenAll(candidatesTask, daysTask);
        IReadOnlyCollection<TripParkCandidate> candidates = await candidatesTask;
        IReadOnlyCollection<TripDayPlan> days = await daysTask;
        string[] parkIds = candidates.Select(static candidate => candidate.ParkId)
            .Concat(days.Select(static day => day.ParkId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(
            parkIds,
            cancellationToken);
        Dictionary<string, string?> parkNames = parks
            .Where(static park => park.Id is not null)
            .ToDictionary(static park => park.Id!, static park => park.Name, StringComparer.Ordinal);
        return new TripProgramResult(
            candidates.Select(candidate => ToCandidateResult(
                candidate,
                parkNames.GetValueOrDefault(candidate.ParkId))).ToArray(),
            days.Select(day => ToDayResult(
                day,
                parkNames.GetValueOrDefault(day.ParkId))).ToArray());
    }

    public static TripParkCandidateResult ToCandidateResult(
        TripParkCandidate candidate,
        string? parkName)
    {
        return new TripParkCandidateResult(
            candidate.Id.Value,
            candidate.ParkId,
            NormalizeParkName(parkName),
            candidate.CandidateDates,
            candidate.Source,
            candidate.State,
            candidate.CollectiveNote,
            candidate.FitSnapshot is null
                ? null
                : new TripFitRecommendationSnapshotResult(
                    candidate.FitSnapshot.MethodVersion,
                    candidate.FitSnapshot.Explanation,
                    candidate.FitSnapshot.CalculatedAtUtc),
            candidate.SortPosition,
            candidate.Version,
            candidate.CreatedAtUtc,
            candidate.UpdatedAtUtc);
    }

    public static TripDayPlanResult ToDayResult(TripDayPlan dayPlan, string? parkName)
    {
        return new TripDayPlanResult(
            dayPlan.Id.Value,
            dayPlan.LocalDate,
            dayPlan.ParkCandidateId.Value,
            dayPlan.ParkId,
            NormalizeParkName(parkName),
            dayPlan.DesiredArrivalTime,
            dayPlan.GroupNote,
            dayPlan.Blocks.Select(static block => new TripDayBlockResult(
                block.Id.Value,
                block.Type,
                block.Title,
                block.Details,
                block.LocalTime,
                block.SortPosition)).ToArray(),
            dayPlan.Version,
            dayPlan.CreatedAtUtc,
            dayPlan.UpdatedAtUtc);
    }

    private static string NormalizeParkName(string? parkName)
    {
        return string.IsNullOrWhiteSpace(parkName) ? "Parc indisponible" : parkName.Trim();
    }
}
