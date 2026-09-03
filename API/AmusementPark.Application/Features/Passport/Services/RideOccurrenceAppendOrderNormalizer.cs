using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class RideOccurrenceAppendOrderNormalizer
{
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;

    public RideOccurrenceAppendOrderNormalizer(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
    {
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
    }

    public async Task<bool> TryNormalizeAsync(
        Visit visit,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RideOccurrence> occurrences;
        try
        {
            occurrences = await RideOccurrenceOrderLoader.LoadAllAsync(
                this.occurrenceRepository,
                visit.Id,
                visit.UserId,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        if (occurrences.Count == 0)
        {
            return false;
        }

        RideOccurrenceOrderPlan plan;
        try
        {
            plan = RideOccurrenceOrderPlanner.PlanNormalization(occurrences);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or OverflowException)
        {
            return false;
        }

        if (plan.Changes.Count == 0)
        {
            return false;
        }

        IReadOnlyList<RideOccurrence> ordered = occurrences
            .OrderBy(static occurrence => occurrence.SortPosition)
            .ThenBy(static occurrence => occurrence.CreatedAtUtc)
            .ThenBy(static occurrence => occurrence.Id.Value, StringComparer.Ordinal)
            .ToArray();
        RideOccurrence first = ordered[0];
        long expectedFirstVersion = first.Version;
        string normalizationOperationId = BuildOperationId(clientOperationId, ordered);
        Dictionary<RideOccurrenceId, RideOccurrence> byId = occurrences.ToDictionary(
            static occurrence => occurrence.Id);
        DateTime nowUtc = this.clock.UtcNow;
        List<RideOccurrenceVersionedChange> changes =
            new List<RideOccurrenceVersionedChange>();
        try
        {
            foreach (RideOccurrenceOrderPosition position in plan.Changes)
            {
                RideOccurrence occurrence = byId[position.OccurrenceId];
                long expectedVersion = occurrence.Version;
                long previousSortPosition = occurrence.SortPosition;
                occurrence.MoveTo(position.SortPosition, nowUtc);
                changes.Add(new RideOccurrenceVersionedChange(
                    occurrence,
                    expectedVersion,
                    previousSortPosition));
            }
        }
        catch (RideOccurrenceValidationException)
        {
            return false;
        }

        RideOccurrenceReorderRequest request = new RideOccurrenceReorderRequest(
            visit.Id,
            visit.UserId,
            first.Id,
            expectedFirstVersion,
            null,
            RideOccurrencePlacement.First);
        IdempotentRideOccurrenceReorderResult result =
            await this.occurrenceRepository.ReorderIdempotentAsync(
                request,
                changes,
                plan.Guards,
                byId[first.Id],
                true,
                nowUtc,
                normalizationOperationId,
                cancellationToken);
        return result.Status is IdempotentRideOccurrenceReorderStatus.Applied
            or IdempotentRideOccurrenceReorderStatus.Replayed;
    }

    private static string BuildOperationId(
        string clientOperationId,
        IReadOnlyCollection<RideOccurrence> orderedOccurrences)
    {
        StringBuilder fingerprint = new StringBuilder(clientOperationId.Length + 128);
        fingerprint.Append(clientOperationId).Append('\n');
        foreach (RideOccurrence occurrence in orderedOccurrences)
        {
            fingerprint
                .Append(occurrence.Id.Value)
                .Append(':')
                .Append(occurrence.Version.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(occurrence.SortPosition.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint.ToString()));
        return $"internal-passport-append-normalization-v1:{Convert.ToHexString(hash)}";
    }
}
