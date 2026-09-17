using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripProgramResultFactory
{
    public const int MaximumConsistentReadAttempts = 3;

    private readonly ITripPlanRepository tripPlanRepository;
    private readonly ITripParkCandidateRepository candidateRepository;
    private readonly ITripDayPlanRepository dayPlanRepository;
    private readonly IParkRepository parkRepository;

    public TripProgramResultFactory(
        ITripPlanRepository tripPlanRepository,
        ITripParkCandidateRepository candidateRepository,
        ITripDayPlanRepository dayPlanRepository,
        IParkRepository parkRepository)
    {
        this.tripPlanRepository = tripPlanRepository
            ?? throw new ArgumentNullException(nameof(tripPlanRepository));
        this.candidateRepository = candidateRepository
            ?? throw new ArgumentNullException(nameof(candidateRepository));
        this.dayPlanRepository = dayPlanRepository
            ?? throw new ArgumentNullException(nameof(dayPlanRepository));
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
    }

    public async Task<ApplicationResult<TripProgramResult>> BuildAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TripParkCandidate>? candidates = null;
        IReadOnlyCollection<TripDayPlan>? days = null;
        for (int attempt = 0; attempt < MaximumConsistentReadAttempts; attempt++)
        {
            long? sequenceBefore = await this.tripPlanRepository.GetProgramReadSequenceAsync(
                tripPlanId,
                cancellationToken);
            if (!sequenceBefore.HasValue)
            {
                return ApplicationResult<TripProgramResult>.Failure(
                    TripPlanApplicationErrors.NotFound());
            }

            Task<IReadOnlyCollection<TripParkCandidate>> candidatesTask =
                this.candidateRepository.ListAsync(tripPlanId, cancellationToken);
            Task<IReadOnlyCollection<TripDayPlan>> daysTask =
                this.dayPlanRepository.ListAsync(tripPlanId, cancellationToken);
            await Task.WhenAll(candidatesTask, daysTask);
            candidates = await candidatesTask;
            days = await daysTask;
            long? sequenceAfter = await this.tripPlanRepository.GetProgramReadSequenceAsync(
                tripPlanId,
                cancellationToken);
            if (sequenceAfter == sequenceBefore)
            {
                break;
            }

            candidates = null;
            days = null;
        }

        if (candidates is null || days is null)
        {
            return ApplicationResult<TripProgramResult>.Failure(
                TripPlanApplicationErrors.ChildMutationUnavailable());
        }

        string[] parkIds = candidates.Select(static candidate => candidate.ParkId)
            .Concat(days.Select(static day => day.ParkId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(
            parkIds,
            cancellationToken);
        Dictionary<string, Park> parksById = parks
            .Where(static park => park.Id is not null)
            .ToDictionary(static park => park.Id!, StringComparer.Ordinal);
        return ApplicationResult<TripProgramResult>.Success(new TripProgramResult(
            candidates.Select(candidate => ToCandidateResult(
                candidate,
                parksById.GetValueOrDefault(candidate.ParkId))).ToArray(),
            days.Select(day => ToDayResult(
                day,
                parksById.GetValueOrDefault(day.ParkId))).ToArray()));
    }

    public static TripParkCandidateResult ToCandidateResult(
        TripParkCandidate candidate,
        Park? park)
    {
        string? normalizedParkName = ResolveAvailableParkName(park);
        return new TripParkCandidateResult(
            candidate.Id.Value,
            candidate.ParkId,
            normalizedParkName,
            normalizedParkName is not null,
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

    public static TripDayPlanResult ToDayResult(TripDayPlan dayPlan, Park? park)
    {
        string? normalizedParkName = ResolveAvailableParkName(park);
        return new TripDayPlanResult(
            dayPlan.Id.Value,
            dayPlan.LocalDate,
            dayPlan.ParkCandidateId.Value,
            dayPlan.ParkId,
            normalizedParkName,
            normalizedParkName is not null,
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

    private static string? ResolveAvailableParkName(Park? park)
    {
        return park?.IsPubliclyDiscoverable() == true
            ? park.Name!.Trim()
            : null;
    }
}
