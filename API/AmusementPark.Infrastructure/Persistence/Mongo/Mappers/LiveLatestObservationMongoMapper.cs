using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

public static class LiveLatestObservationMongoMapper
{
    public static LiveLatestObservationDocument ToDocument(this LiveLatestObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        LiveObservationProvenance provenance = observation.Provenance;
        LiveFreshnessPolicy freshnessPolicy = observation.FreshnessPolicy;
        LiveFreshnessAssessment freshness = freshnessPolicy.Assess(
            provenance.ObservedAtUtc,
            provenance.NormalizedAtUtc);
        return new LiveLatestObservationDocument
        {
            Id = BuildId(provenance.SourceId, observation.Target),
            CreatedAt = provenance.NormalizedAtUtc,
            UpdatedAt = provenance.NormalizedAtUtc,
            SourceId = provenance.SourceId.Value,
            Target = ToDocument(observation.Target),
            Status = observation.Status,
            Queues = observation.Queues.Select(ToDocument).ToList(),
            Provenance = new LiveObservationProvenanceDocument
            {
                ExternalTargetId = provenance.ExternalTargetId,
                ObservedAtUtc = provenance.ObservedAtUtc,
                ObservedAtUtcTicks = provenance.ObservedAtUtc.Ticks,
                ReceivedAtUtc = provenance.ReceivedAtUtc,
                ReceivedAtUtcTicks = provenance.ReceivedAtUtc.Ticks,
                NormalizedAtUtc = provenance.NormalizedAtUtc,
                NormalizedAtUtcTicks = provenance.NormalizedAtUtc.Ticks,
                CorrelationId = provenance.CorrelationId,
                AdapterVersion = provenance.AdapterVersion,
                MappingVersion = provenance.MappingVersion,
                Confidence = provenance.Confidence,
                UsagePolicyVersion = provenance.UsagePolicyVersion,
                TransformationVersion = provenance.TransformationVersion,
            },
            FreshnessPolicy = new LiveFreshnessPolicyDocument
            {
                Version = freshnessPolicy.Version,
                FreshUntilMilliseconds = (long)freshnessPolicy.FreshUntil.TotalMilliseconds,
                AgingUntilMilliseconds = (long)freshnessPolicy.AgingUntil.TotalMilliseconds,
                StaleUntilMilliseconds = (long)freshnessPolicy.StaleUntil.TotalMilliseconds,
                AcceptedFutureSkewMilliseconds =
                    (long)freshnessPolicy.AcceptedFutureSkew.TotalMilliseconds,
            },
            ExpiresAtUtc = freshness.ExpiresAtUtc,
            PayloadSha256 = observation.PayloadSha256,
            HasStatusQueueConflict = observation.HasStatusQueueConflict,
        };
    }

    public static LiveLatestObservation ToDomain(this LiveLatestObservationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        LiveObservationProvenanceDocument provenance = document.Provenance;
        LiveFreshnessPolicyDocument freshnessPolicy = document.FreshnessPolicy;
        DateTime receivedAtUtc = RestoreExactUtc(
            provenance.ReceivedAtUtc,
            provenance.ReceivedAtUtcTicks);
        DateTime normalizedAtUtc = provenance.NormalizedAtUtcTicks > 0
            ? RestoreExactUtc(provenance.NormalizedAtUtc, provenance.NormalizedAtUtcTicks)
            : MaxUtc(DateTime.SpecifyKind(provenance.NormalizedAtUtc, DateTimeKind.Utc), receivedAtUtc);
        return new LiveLatestObservation(
            new LiveTargetReference(
                document.Target.Type,
                document.Target.Id,
                document.Target.ParkId,
                document.Target.DisplayName,
                document.Target.ParkDisplayName,
                document.Target.CountryCode),
            document.Status,
            document.Queues.Select(ToDomain).ToList(),
            new LiveObservationProvenance(
                LiveDataSourceId.Parse(document.SourceId),
                provenance.ExternalTargetId,
                RestoreExactUtc(provenance.ObservedAtUtc, provenance.ObservedAtUtcTicks),
                receivedAtUtc,
                normalizedAtUtc,
                provenance.CorrelationId,
                provenance.AdapterVersion,
                provenance.MappingVersion,
                provenance.Confidence,
                provenance.UsagePolicyVersion,
                provenance.TransformationVersion),
            new LiveFreshnessPolicy(
                freshnessPolicy.Version,
                TimeSpan.FromMilliseconds(freshnessPolicy.FreshUntilMilliseconds),
                TimeSpan.FromMilliseconds(freshnessPolicy.AgingUntilMilliseconds),
                TimeSpan.FromMilliseconds(freshnessPolicy.StaleUntilMilliseconds),
                TimeSpan.FromMilliseconds(freshnessPolicy.AcceptedFutureSkewMilliseconds)),
            document.PayloadSha256);
    }

    private static string BuildId(LiveDataSourceId sourceId, LiveTargetReference target)
    {
        string source = sourceId.Value;
        return $"{source.Length}:{source}|{(int)target.Type}|{target.Id.Length}:{target.Id}";
    }

    private static LiveTargetReferenceDocument ToDocument(LiveTargetReference target)
    {
        return new LiveTargetReferenceDocument
        {
            Type = target.Type,
            Id = target.Id,
            ParkId = target.ParkId,
            DisplayName = target.DisplayName,
            ParkDisplayName = target.ParkDisplayName,
            CountryCode = target.CountryCode,
        };
    }

    private static LiveQueueObservationDocument ToDocument(LiveQueueObservation queue)
    {
        return new LiveQueueObservationDocument
        {
            Kind = queue.Kind,
            WaitTimeMinutes = queue.WaitTimeMinutes,
            IsEstimated = queue.IsEstimated,
            Availability = queue.Availability,
            ReturnStartUtc = queue.ReturnStartUtc,
            ReturnEndUtc = queue.ReturnEndUtc,
            CurrentGroupStart = queue.CurrentGroupStart,
            CurrentGroupEnd = queue.CurrentGroupEnd,
            NextAllocationUtc = queue.NextAllocationUtc,
            PriceMinorUnits = queue.PriceMinorUnits,
            CurrencyCode = queue.CurrencyCode,
        };
    }

    private static LiveQueueObservation ToDomain(LiveQueueObservationDocument queue)
    {
        return new LiveQueueObservation(
            queue.Kind,
            queue.WaitTimeMinutes,
            queue.IsEstimated,
            queue.Availability,
            queue.ReturnStartUtc,
            queue.ReturnEndUtc,
            queue.CurrentGroupStart,
            queue.CurrentGroupEnd,
            queue.NextAllocationUtc,
            queue.PriceMinorUnits,
            queue.CurrencyCode);
    }

    private static DateTime RestoreExactUtc(DateTime fallback, long ticks)
    {
        return ticks > 0
            ? new DateTime(ticks, DateTimeKind.Utc)
            : DateTime.SpecifyKind(fallback, DateTimeKind.Utc);
    }

    private static DateTime MaxUtc(DateTime first, DateTime second)
    {
        return first >= second ? first : second;
    }
}
