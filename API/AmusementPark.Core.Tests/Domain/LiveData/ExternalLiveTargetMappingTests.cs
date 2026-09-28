using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class ExternalLiveTargetMappingTests
{
    private static readonly DateTime DiscoveredAtUtc = new(
        2026,
        9,
        28,
        12,
        0,
        0,
        DateTimeKind.Utc);

    [Fact]
    public void CreateCandidate_WithoutSuggestion_ShouldNotBeEligibleForLiveUse()
    {
        ExternalLiveTargetMapping mapping = CreateCandidate(null);

        Assert.Equal(LiveMappingStatus.Candidate, mapping.Status);
        Assert.Equal(1, mapping.Revision);
        Assert.Null(mapping.SupersedesRevision);
        Assert.False(mapping.IsEligibleForLiveUse);
        Assert.Null(mapping.ReviewedByUserId);
    }

    [Fact]
    public void Verify_ShouldCreateHumanReviewedHighConfidenceRevision()
    {
        ExternalLiveTargetMapping candidate = CreateCandidate(CreateTarget("item-1"));
        DateTime reviewedAtUtc = DiscoveredAtUtc.AddMinutes(5);

        ExternalLiveTargetMapping verified = candidate.Verify(
            CreateTarget("item-1"),
            "admin-1",
            "Identifiants et parc contrôlés.",
            reviewedAtUtc);

        Assert.Equal(LiveMappingStatus.Verified, verified.Status);
        Assert.Equal(LiveMappingConfidence.High, verified.Confidence);
        Assert.Equal(2, verified.Revision);
        Assert.Equal(1, verified.SupersedesRevision);
        Assert.Equal(reviewedAtUtc, verified.ValidFromUtc);
        Assert.True(verified.IsEligibleForLiveUse);
        Assert.Equal($"{verified.Id:N}:2", verified.Version);
    }

    [Fact]
    public void Correct_ShouldKeepImmutableRevisionChainAndChangeTarget()
    {
        ExternalLiveTargetMapping verified = CreateCandidate(CreateTarget("item-1"))
            .Verify(
                CreateTarget("item-1"),
                "admin-1",
                null,
                DiscoveredAtUtc.AddMinutes(5));

        ExternalLiveTargetMapping corrected = verified.Correct(
            CreateTarget("item-2"),
            "admin-2",
            "La source avait été reliée au mauvais élément.",
            DiscoveredAtUtc.AddMinutes(10));

        Assert.Equal("item-1", verified.Target?.Id);
        Assert.Equal("item-2", corrected.Target?.Id);
        Assert.Equal(3, corrected.Revision);
        Assert.Equal(2, corrected.SupersedesRevision);
        Assert.True(corrected.IsEligibleForLiveUse);
    }

    [Fact]
    public void Correct_WithoutReviewNote_ShouldRejectMapping()
    {
        ExternalLiveTargetMapping verified = CreateCandidate(CreateTarget("item-1"))
            .Verify(
                CreateTarget("item-1"),
                "admin-1",
                null,
                DiscoveredAtUtc.AddMinutes(5));

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => verified.Correct(
                CreateTarget("item-2"),
                "admin-2",
                string.Empty,
                DiscoveredAtUtc.AddMinutes(10)));

        Assert.Equal(LiveDataErrorCodes.InvalidMapping, exception.Code);
    }

    [Fact]
    public void Suspend_ShouldCloseValidityAndPreventLiveUse()
    {
        ExternalLiveTargetMapping verified = CreateCandidate(null)
            .Verify(
                CreateTarget("item-1"),
                "admin-1",
                null,
                DiscoveredAtUtc.AddMinutes(5));
        DateTime suspendedAtUtc = DiscoveredAtUtc.AddMinutes(15);

        ExternalLiveTargetMapping suspended = verified.Suspend(
            "admin-2",
            "Source incohérente.",
            suspendedAtUtc);

        Assert.Equal(LiveMappingStatus.Suspended, suspended.Status);
        Assert.Equal(suspendedAtUtc, suspended.ValidToUtc);
        Assert.False(suspended.IsEligibleForLiveUse);
    }

    [Fact]
    public void Reject_FromVerifiedMapping_ShouldRejectTransition()
    {
        ExternalLiveTargetMapping verified = CreateCandidate(null)
            .Verify(
                CreateTarget("item-1"),
                "admin-1",
                null,
                DiscoveredAtUtc.AddMinutes(5));

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => verified.Reject(
                "admin-2",
                "Rejet tardif.",
                DiscoveredAtUtc.AddMinutes(10)));

        Assert.Equal(LiveDataErrorCodes.InvalidMappingTransition, exception.Code);
    }

    [Fact]
    public void Verify_WhenCountriesDiffer_ShouldRejectMapping()
    {
        ExternalLiveTargetMapping candidate = CreateCandidate(null);
        LiveTargetReference frenchTarget = new LiveTargetReference(
            LiveTargetType.ParkItem,
            "item-1",
            "park-1",
            "Attraction",
            "Parc",
            "FR");

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => candidate.Verify(
                frenchTarget,
                "admin-1",
                null,
                DiscoveredAtUtc.AddMinutes(5)));

        Assert.Equal(LiveDataErrorCodes.MappingCountryMismatch, exception.Code);
    }

    [Fact]
    public void Constructor_WithBrokenRevisionChain_ShouldRejectMapping()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new ExternalLiveTargetMapping(
                Guid.NewGuid(),
                LiveDataSourceId.Parse("themeparks-wiki"),
                CreateExternalTarget(),
                null,
                LiveMappingStatus.Candidate,
                LiveMappingConfidence.Low,
                DiscoveredAtUtc,
                null,
                2,
                null,
                null,
                null,
                DiscoveredAtUtc));

        Assert.Equal(LiveDataErrorCodes.InvalidRevision, exception.Code);
    }

    private static ExternalLiveTargetMapping CreateCandidate(LiveTargetReference? suggestedTarget)
    {
        return ExternalLiveTargetMapping.CreateCandidate(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            LiveDataSourceId.Parse("themeparks-wiki"),
            CreateExternalTarget(),
            suggestedTarget,
            suggestedTarget is null ? LiveMappingConfidence.Low : LiveMappingConfidence.Medium,
            DiscoveredAtUtc);
    }

    private static ExternalLiveTargetDescriptor CreateExternalTarget()
    {
        return new ExternalLiveTargetDescriptor(
            LiveTargetType.ParkItem,
            "external-item-1",
            "external-park-1",
            "Black Mamba",
            "Phantasialand",
            "DE");
    }

    private static LiveTargetReference CreateTarget(string targetId)
    {
        return new LiveTargetReference(
            LiveTargetType.ParkItem,
            targetId,
            "park-1",
            "Black Mamba",
            "Phantasialand",
            "DE");
    }
}
