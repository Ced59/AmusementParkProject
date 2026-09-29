using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

public static class LiveHistoryMongoMapper
{
    public static LiveLatestObservationDocument ToRawHistoryDocument(
        this LiveLatestObservation observation,
        LiveHistoryRetentionPolicy retentionPolicy)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(retentionPolicy);
        LiveLatestObservationDocument document = observation.ToDocument();
        document.Id = BuildObservationId(document.Id, observation.Provenance.ObservedAtUtc.Ticks);
        document.ExpiresAtUtc = retentionPolicy.GetRawExpirationUtc(
            observation.Provenance.NormalizedAtUtc);
        return document;
    }

    public static LiveHistoryBucketDocument ToHistoryBucketDocument(
        this LiveLatestObservation observation,
        LiveHistoryRetentionPolicy retentionPolicy)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(retentionPolicy);
        LiveLatestObservationDocument latest = observation.ToDocument();
        DateTime bucketStartUtc = retentionPolicy.GetBucketStartUtc(
            observation.Provenance.ObservedAtUtc);
        DateTime bucketEndUtc = bucketStartUtc.Add(retentionPolicy.BucketDuration);
        return new LiveHistoryBucketDocument
        {
            Id = BuildObservationId(latest.Id, bucketStartUtc.Ticks),
            CreatedAt = observation.Provenance.NormalizedAtUtc,
            UpdatedAt = observation.Provenance.NormalizedAtUtc,
            SourceId = observation.Provenance.SourceId.Value,
            Target = latest.Target,
            BucketStartUtc = bucketStartUtc,
            BucketEndUtc = bucketEndUtc,
            BucketDurationMilliseconds = (long)retentionPolicy.BucketDuration.TotalMilliseconds,
            UsagePolicyVersion = observation.Provenance.UsagePolicyVersion,
            ExpiresAtUtc = retentionPolicy.GetAggregateExpirationUtc(bucketStartUtc),
            Samples = new List<LiveHistoryBucketSampleDocument>
            {
                new LiveHistoryBucketSampleDocument
                {
                    SampleId = observation.Provenance.ObservedAtUtc.Ticks.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    ObservedAtUtc = observation.Provenance.ObservedAtUtc,
                    ObservedAtUtcTicks = observation.Provenance.ObservedAtUtc.Ticks,
                    ReceivedAtUtc = observation.Provenance.ReceivedAtUtc,
                    ReceivedAtUtcTicks = observation.Provenance.ReceivedAtUtc.Ticks,
                    Status = observation.Status,
                    Queues = latest.Queues,
                },
            },
        };
    }

    private static string BuildObservationId(string targetNaturalId, long timeBucketTicks)
    {
        return $"{targetNaturalId.Length}:{targetNaturalId}|{timeBucketTicks}";
    }
}
