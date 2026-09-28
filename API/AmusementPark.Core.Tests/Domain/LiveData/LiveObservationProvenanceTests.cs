using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveObservationProvenanceTests
{
    private static readonly DateTime ObservedAtUtc = new(
        2026,
        9,
        28,
        12,
        0,
        0,
        DateTimeKind.Utc);

    [Fact]
    public void Constructor_ShouldPreserveTraceableObservationEvidence()
    {
        LiveObservationProvenance provenance = CreateProvenance(
            ObservedAtUtc,
            ObservedAtUtc.AddSeconds(2),
            ObservedAtUtc.AddSeconds(3),
            LiveDataConfidence.High);

        Assert.Equal("pilot", provenance.SourceId.Value);
        Assert.Equal("external-ride-1", provenance.ExternalTargetId);
        Assert.Equal(ObservedAtUtc, provenance.ObservedAtUtc);
        Assert.Equal("adapter-1", provenance.AdapterVersion);
        Assert.Equal("mapping-1", provenance.MappingVersion);
        Assert.Equal("terms-1", provenance.UsagePolicyVersion);
        Assert.Equal("raw-to-latest-1", provenance.TransformationVersion);
        Assert.Equal(LiveDataConfidence.High, provenance.Confidence);
    }

    [Fact]
    public void Constructor_WhenNormalizationPredatesReception_ShouldRejectObservation()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreateProvenance(
                ObservedAtUtc,
                ObservedAtUtc.AddSeconds(3),
                ObservedAtUtc.AddSeconds(2),
                LiveDataConfidence.Medium));

        Assert.Equal(LiveDataErrorCodes.InconsistentTimeline, exception.Code);
    }

    [Fact]
    public void Constructor_WhenConfidenceIsUnknown_ShouldRejectObservation()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreateProvenance(
                ObservedAtUtc,
                ObservedAtUtc.AddSeconds(2),
                ObservedAtUtc.AddSeconds(3),
                (LiveDataConfidence)999));

        Assert.Equal(LiveDataErrorCodes.InvalidEnum, exception.Code);
    }

    [Fact]
    public void Constructor_WhenObservedTimestampIsNotUtc_ShouldRejectObservation()
    {
        DateTime localObservation = DateTime.SpecifyKind(ObservedAtUtc, DateTimeKind.Local);

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreateProvenance(
                localObservation,
                ObservedAtUtc.AddSeconds(2),
                ObservedAtUtc.AddSeconds(3),
                LiveDataConfidence.High));

        Assert.Equal(LiveDataErrorCodes.InvalidTimestamp, exception.Code);
    }

    private static LiveObservationProvenance CreateProvenance(
        DateTime observedAtUtc,
        DateTime receivedAtUtc,
        DateTime normalizedAtUtc,
        LiveDataConfidence confidence)
    {
        return new LiveObservationProvenance(
            LiveDataSourceId.Parse("pilot"),
            "external-ride-1",
            observedAtUtc,
            receivedAtUtc,
            normalizedAtUtc,
            "correlation-1",
            "adapter-1",
            "mapping-1",
            confidence,
            "terms-1",
            "raw-to-latest-1");
    }
}
