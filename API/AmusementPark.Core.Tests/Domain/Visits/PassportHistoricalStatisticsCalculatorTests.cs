using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportHistoricalStatisticsCalculatorTests
{
    [Fact]
    public void Calculate_WithCanonicalChanges_ShouldBuildEvidenceBasedStatistics()
    {
        PassportHistoricalTargetStateObservation oldTarget = Target(
            "item-1",
            "Old Name",
            "DarkRide",
            HistoricalOperationalState.KnownOpen);
        PassportHistoricalTargetStateObservation renamedTarget = Target(
            "item-1",
            "New Name",
            "DarkRide",
            HistoricalOperationalState.KnownOpen);
        PassportHistoricalTargetStateObservation closedTarget = Target(
            "item-1",
            "New Name",
            "DarkRide",
            HistoricalOperationalState.KnownClosed);
        PassportHistoricalVisitContextObservation firstVisit = Visit(
            "visit-1",
            2001,
            oldTarget);
        PassportHistoricalVisitContextObservation secondVisit = Visit(
            "visit-2",
            2008,
            renamedTarget);

        PassportHistoricalStatistics result = PassportHistoricalStatisticsCalculator.Calculate(
            new[] { firstVisit, secondVisit },
            new[]
            {
                Ride("ride-1", firstVisit, oldTarget, closedTarget),
                Ride("ride-2", secondVisit, renamedTarget, closedTarget),
            });

        Assert.Equal(2001, result.FirstVisitYear);
        Assert.Equal(2, result.CompletedRideCount);
        Assert.Equal(2, result.CanonicallyResolvedRideCount);
        Assert.Equal(1d, result.CanonicalCoverageRate);
        Assert.Equal(1, result.ParkCountAcrossMultipleEras);
        Assert.Equal(2, Assert.Single(result.ParksAcrossEras).CanonicalEraCount);
        Assert.Equal(2, Assert.Single(result.DisappearedAttractions).CompletedRideCount);
        PassportHistoricalTransformationStatistic transformation =
            Assert.Single(result.Transformations);
        Assert.Equal("New Name", transformation.CurrentName);
        Assert.Equal(new[] { "Old Name" }, transformation.NamesAtVisit);
        Assert.Equal(2, result.HistoricalNames.Count);
        Assert.Equal("DarkRide", Assert.Single(result.HistoricalCategories).Category);
    }

    [Fact]
    public void Calculate_WithFallbackContext_ShouldExcludeItFromCanonicalEvidence()
    {
        PassportHistoricalTargetStateObservation fallback = new(
            "item-1",
            "Current Name",
            "Attraction",
            HistoricalOperationalState.Unknown,
            false);
        PassportHistoricalVisitContextObservation visit = Visit("visit-1", 2026, fallback);

        PassportHistoricalStatistics result = PassportHistoricalStatisticsCalculator.Calculate(
            new[] { visit },
            new[] { Ride("ride-1", visit, fallback, fallback) });

        Assert.Equal(1, result.CompletedRideCount);
        Assert.Equal(0, result.CanonicallyResolvedRideCount);
        Assert.Equal(0d, result.CanonicalCoverageRate);
        Assert.Empty(result.DisappearedAttractions);
        Assert.Empty(result.Transformations);
        Assert.Empty(result.HistoricalNames);
        Assert.Empty(result.HistoricalCategories);
        Assert.Equal(0, Assert.Single(result.ParksAcrossEras).CanonicalEraCount);
    }

    [Fact]
    public void Calculate_WithNonCompletedRide_ShouldNotClaimHistoricalVisit()
    {
        PassportHistoricalTargetStateObservation target = Target(
            "item-1",
            "Attraction",
            "Attraction",
            HistoricalOperationalState.KnownOpen);
        PassportHistoricalVisitContextObservation visit = Visit("visit-1", 2010, target);
        PassportHistoricalRideContextObservation ride = new(
            "ride-1",
            visit.VisitId,
            visit.ParkId,
            target.ParkItemId,
            visit.VisitDate,
            RideOccurrenceStatus.MissedClosed,
            target,
            target);

        PassportHistoricalStatistics result = PassportHistoricalStatisticsCalculator.Calculate(
            new[] { visit },
            new[] { ride });

        Assert.Equal(0, result.CompletedRideCount);
        Assert.Empty(result.HistoricalNames);
    }

    private static PassportHistoricalVisitContextObservation Visit(
        string visitId,
        int year,
        params PassportHistoricalTargetStateObservation[] targets)
    {
        return new PassportHistoricalVisitContextObservation(
            visitId,
            "park-1",
            VisitDate.ForYear(year),
            HistoricalCoverageStatus.HighConfidence,
            targets);
    }

    private static PassportHistoricalRideContextObservation Ride(
        string rideId,
        PassportHistoricalVisitContextObservation visit,
        PassportHistoricalTargetStateObservation targetAtVisit,
        PassportHistoricalTargetStateObservation currentTarget)
    {
        return new PassportHistoricalRideContextObservation(
            rideId,
            visit.VisitId,
            visit.ParkId,
            targetAtVisit.ParkItemId,
            visit.VisitDate,
            RideOccurrenceStatus.Completed,
            targetAtVisit,
            currentTarget);
    }

    private static PassportHistoricalTargetStateObservation Target(
        string parkItemId,
        string name,
        string category,
        HistoricalOperationalState state)
    {
        return new PassportHistoricalTargetStateObservation(
            parkItemId,
            name,
            category,
            state,
            true);
    }
}
