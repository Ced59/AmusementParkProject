using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveQualityIncidentTests
{
    private static readonly DateTime ReceivedAtUtc =
        new DateTime(2026, 9, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_ShouldRequireObservationForReplayableReason()
    {
        Assert.Throws<ArgumentException>(() => CreateIncident(
            LiveQualityIncidentReason.UnmappedTarget,
            observation: null,
            diagnosticCode: null));
    }

    [Fact]
    public void Resolve_ShouldKeepPayloadAndAddAuditMetadata()
    {
        LiveQualityIncident pending = CreateIncident(
            LiveQualityIncidentReason.UnmappedTarget,
            CreateObservation(),
            diagnosticCode: null);

        LiveQualityIncident resolved = pending.Resolve(
            ReceivedAtUtc.AddMinutes(2),
            "admin-1");

        Assert.Equal(LiveQualityIncidentStatus.Resolved, resolved.Status);
        Assert.Equal("admin-1", resolved.ResolvedByUserId);
        Assert.Equal(pending.Observation, resolved.Observation);
        Assert.False(resolved.IsReplayable);
    }

    [Fact]
    public void ProviderDiagnostic_ShouldAllowMinimizedIncidentWithoutObservation()
    {
        LiveQualityIncident incident = CreateIncident(
            LiveQualityIncidentReason.ProviderDiagnostic,
            observation: null,
            LiveProviderCode());

        Assert.Null(incident.Observation);
        Assert.Equal(LiveProviderCode(), incident.DiagnosticCode);
        Assert.False(incident.IsReplayable);
    }

    private static LiveQualityIncident CreateIncident(
        LiveQualityIncidentReason reason,
        ExternalLiveObservation? observation,
        string? diagnosticCode)
    {
        return new LiveQualityIncident(
            Guid.NewGuid(),
            LiveDataSourceId.Parse("source"),
            observation,
            reason,
            diagnosticCode,
            observation?.ExternalTargetId,
            null,
            ReceivedAtUtc,
            ReceivedAtUtc.AddSeconds(1),
            ReceivedAtUtc.AddDays(7),
            "correlation",
            "adapter-1",
            "usage-1",
            "transform-1",
            LiveDataConfidence.Medium,
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(1)),
            new string('a', 64));
    }

    private static ExternalLiveObservation CreateObservation()
    {
        return new ExternalLiveObservation(
            "external-1",
            "Attraction",
            LiveTargetType.ParkItem,
            LiveOperationalStatus.Open,
            ReceivedAtUtc.AddMinutes(-1));
    }

    private static string LiveProviderCode()
    {
        return "live-provider.invalid-observation";
    }
}
