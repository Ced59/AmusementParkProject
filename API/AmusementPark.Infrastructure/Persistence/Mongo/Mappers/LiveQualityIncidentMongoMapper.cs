using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

public static class LiveQualityIncidentMongoMapper
{
    public static LiveQualityIncidentDocument ToDocument(this LiveQualityIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        return new LiveQualityIncidentDocument
        {
            Id = BuildDocumentId(incident),
            IncidentId = incident.Id.ToString("N"),
            CreatedAt = incident.DetectedAtUtc,
            UpdatedAt = incident.ResolvedAtUtc ?? incident.DetectedAtUtc,
            SourceId = incident.SourceId.Value,
            Observation = incident.Observation is null ? null : ToDocument(incident.Observation),
            Reason = incident.Reason,
            DiagnosticCode = incident.DiagnosticCode,
            DiagnosticExternalTargetId = incident.DiagnosticExternalTargetId,
            DiagnosticField = incident.DiagnosticField,
            ReceivedAtUtc = incident.ReceivedAtUtc,
            ReceivedAtUtcTicks = incident.ReceivedAtUtc.Ticks,
            DetectedAtUtc = incident.DetectedAtUtc,
            DetectedAtUtcTicks = incident.DetectedAtUtc.Ticks,
            ExpiresAtUtc = incident.ExpiresAtUtc,
            CorrelationId = incident.CorrelationId,
            AdapterVersion = incident.AdapterVersion,
            UsagePolicyVersion = incident.UsagePolicyVersion,
            TransformationVersion = incident.TransformationVersion,
            Confidence = incident.Confidence,
            FreshnessPolicy = ToDocument(incident.FreshnessPolicy),
            PayloadSha256 = incident.PayloadSha256,
            Status = incident.Status,
            ResolvedAtUtc = incident.ResolvedAtUtc,
            ResolvedByUserId = incident.ResolvedByUserId,
            ReplayAttemptCount = incident.ReplayAttemptCount,
            LastReplayAttemptAtUtc = incident.LastReplayAttemptAtUtc,
        };
    }

    public static LiveQualityIncident ToDomain(this LiveQualityIncidentDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new LiveQualityIncident(
            Guid.ParseExact(document.IncidentId, "N"),
            LiveDataSourceId.Parse(document.SourceId),
            document.Observation is null ? null : ToDomain(document.Observation),
            document.Reason,
            document.DiagnosticCode,
            document.DiagnosticExternalTargetId,
            document.DiagnosticField,
            new DateTime(document.ReceivedAtUtcTicks, DateTimeKind.Utc),
            new DateTime(document.DetectedAtUtcTicks, DateTimeKind.Utc),
            AsUtc(document.ExpiresAtUtc),
            document.CorrelationId,
            document.AdapterVersion,
            document.UsagePolicyVersion,
            document.TransformationVersion,
            document.Confidence,
            ToDomain(document.FreshnessPolicy),
            document.PayloadSha256,
            document.Status,
            document.ResolvedAtUtc.HasValue ? AsUtc(document.ResolvedAtUtc.Value) : null,
            document.ResolvedByUserId,
            document.ReplayAttemptCount,
            document.LastReplayAttemptAtUtc.HasValue
                ? AsUtc(document.LastReplayAttemptAtUtc.Value)
                : null);
    }

    private static string BuildDocumentId(LiveQualityIncident incident)
    {
        ExternalLiveObservation? observation = incident.Observation;
        string externalTargetId = observation?.ExternalTargetId
            ?? incident.DiagnosticExternalTargetId
            ?? string.Empty;
        long sourceTicks = observation?.SourceUpdatedAtUtc.Ticks
            ?? (incident.PayloadSha256 is null ? incident.ReceivedAtUtc.Ticks : 0L);
        string diagnosticCode = incident.DiagnosticCode ?? string.Empty;
        string diagnosticField = incident.DiagnosticField ?? string.Empty;
        string payloadIdentity = incident.PayloadSha256
            ?? incident.ReceivedAtUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.Join(
            "|",
            Segment(incident.SourceId.Value),
            Segment(externalTargetId),
            sourceTicks,
            (int)incident.Reason,
            Segment(diagnosticCode),
            Segment(diagnosticField),
            Segment(payloadIdentity));
    }

    private static string Segment(string value)
    {
        return $"{value.Length}:{value}";
    }

    private static ExternalLiveObservationDocument ToDocument(ExternalLiveObservation observation)
    {
        return new ExternalLiveObservationDocument
        {
            ExternalTargetId = observation.ExternalTargetId,
            DisplayName = observation.DisplayName,
            TargetType = observation.TargetType,
            Status = observation.Status,
            SourceUpdatedAtUtc = observation.SourceUpdatedAtUtc,
            SourceUpdatedAtUtcTicks = observation.SourceUpdatedAtUtc.Ticks,
            Queues = observation.Queues.Select(ToDocument).ToList(),
        };
    }

    private static ExternalLiveObservation ToDomain(ExternalLiveObservationDocument document)
    {
        DateTime observedAtUtc = new DateTime(document.SourceUpdatedAtUtcTicks, DateTimeKind.Utc);
        return new ExternalLiveObservation(
            document.ExternalTargetId,
            document.DisplayName,
            document.TargetType,
            document.Status,
            observedAtUtc,
            document.Queues.Select(ToDomain).ToArray());
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

    private static LiveQueueObservation ToDomain(LiveQueueObservationDocument document)
    {
        return new LiveQueueObservation(
            document.Kind,
            document.WaitTimeMinutes,
            document.IsEstimated,
            document.Availability,
            document.ReturnStartUtc.HasValue ? AsUtc(document.ReturnStartUtc.Value) : null,
            document.ReturnEndUtc.HasValue ? AsUtc(document.ReturnEndUtc.Value) : null,
            document.CurrentGroupStart,
            document.CurrentGroupEnd,
            document.NextAllocationUtc.HasValue ? AsUtc(document.NextAllocationUtc.Value) : null,
            document.PriceMinorUnits,
            document.CurrencyCode);
    }

    private static LiveFreshnessPolicyDocument ToDocument(LiveFreshnessPolicy policy)
    {
        return new LiveFreshnessPolicyDocument
        {
            Version = policy.Version,
            FreshUntilMilliseconds = (long)policy.FreshUntil.TotalMilliseconds,
            AgingUntilMilliseconds = (long)policy.AgingUntil.TotalMilliseconds,
            StaleUntilMilliseconds = (long)policy.StaleUntil.TotalMilliseconds,
            AcceptedFutureSkewMilliseconds = (long)policy.AcceptedFutureSkew.TotalMilliseconds,
        };
    }

    private static LiveFreshnessPolicy ToDomain(LiveFreshnessPolicyDocument document)
    {
        return new LiveFreshnessPolicy(
            document.Version,
            TimeSpan.FromMilliseconds(document.FreshUntilMilliseconds),
            TimeSpan.FromMilliseconds(document.AgingUntilMilliseconds),
            TimeSpan.FromMilliseconds(document.StaleUntilMilliseconds),
            TimeSpan.FromMilliseconds(document.AcceptedFutureSkewMilliseconds));
    }

    private static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
