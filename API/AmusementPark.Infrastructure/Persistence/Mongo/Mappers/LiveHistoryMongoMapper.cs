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
        document.Id = BuildHistoryId(
            document.Id,
            observation.Provenance.ObservedAtUtc.Ticks,
            observation.Provenance.UsagePolicyVersion,
            retentionPolicy.StorageKey);
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
            Id = BuildHistoryId(
                latest.Id,
                bucketStartUtc.Ticks,
                observation.Provenance.UsagePolicyVersion,
                retentionPolicy.StorageKey),
            CreatedAt = observation.Provenance.NormalizedAtUtc,
            UpdatedAt = observation.Provenance.NormalizedAtUtc,
            SourceId = observation.Provenance.SourceId.Value,
            Target = latest.Target,
            BucketStartUtc = bucketStartUtc,
            BucketEndUtc = bucketEndUtc,
            BucketDurationMilliseconds = (long)retentionPolicy.BucketDuration.TotalMilliseconds,
            UsagePolicyVersion = observation.Provenance.UsagePolicyVersion,
            RetentionPolicyKey = retentionPolicy.StorageKey,
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

    private static string BuildHistoryId(
        string targetNaturalId,
        long timeBucketTicks,
        string usagePolicyVersion,
        string retentionPolicyKey)
    {
        string timedTargetId = BuildObservationId(targetNaturalId, timeBucketTicks);
        string policyTargetId =
            $"{timedTargetId.Length}:{timedTargetId}|{usagePolicyVersion.Length}:{usagePolicyVersion}";
        return $"{policyTargetId.Length}:{policyTargetId}|{retentionPolicyKey.Length}:{retentionPolicyKey}";
    }
}
