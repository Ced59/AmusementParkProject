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
            Id = $"{provenance.SourceId.Value}:{observation.Target.Type}:{observation.Target.Id}",
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
                ReceivedAtUtc = provenance.ReceivedAtUtc,
                NormalizedAtUtc = provenance.NormalizedAtUtc,
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
}
