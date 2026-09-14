using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Geo;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitTravelConvenienceSubscoreEvaluatorTests
{
    private static readonly DateTime EvaluatedAtUtc =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
    private readonly ParkFitTravelConvenienceSubscoreEvaluator evaluator =
        new ParkFitTravelConvenienceSubscoreEvaluator();

    [Fact]
    public void Evaluate_WhenOriginIsNotShared_ShouldRemainNotApplicable()
    {
        ParkFitTravelEvaluation result = this.evaluator.Evaluate(
            null,
            new GeoPoint(50.8d, 6.88d),
            EvaluatedAtUtc);

        Assert.Equal(ParkFitSubscoreState.NotApplicable, result.Subscore.State);
        Assert.Null(result.Distance);
    }

    [Fact]
    public void Evaluate_WhenBothPointsAreKnown_ShouldReturnDatedDirectDistance()
    {
        ParkFitTravelEvaluation result = this.evaluator.Evaluate(
            new GeoPoint(50.6292d, 3.0573d),
            new GeoPoint(50.7998d, 6.8792d),
            EvaluatedAtUtc);

        Assert.Equal(ParkFitSubscoreState.Known, result.Subscore.State);
        Assert.Equal(ParkFitDataConfidence.Medium, result.Subscore.Confidence);
        Assert.Contains(
            ParkFitSubscoreReasonCode.DirectDistanceCalculated,
            result.Subscore.Reasons);
        Assert.NotNull(result.Distance);
        Assert.Equal(ParkFitDistanceMethod.DirectGeodesic, result.Distance.Method);
        Assert.Equal(EvaluatedAtUtc, result.Distance.EvaluatedAtUtc);
        Assert.InRange(result.Distance.DistanceKilometers, 269d, 271d);
        Assert.InRange(result.Subscore.Value!.Value, 72m, 74m);
    }

    [Fact]
    public void Evaluate_WhenDestinationIsFartherThanScale_ShouldFloorScoreAtZero()
    {
        ParkFitTravelEvaluation result = this.evaluator.Evaluate(
            new GeoPoint(0d, 0d),
            new GeoPoint(45d, 90d),
            EvaluatedAtUtc);

        Assert.Equal(0m, result.Subscore.Value);
        Assert.True(result.Distance!.DistanceKilometers > 1000d);
    }

    [Fact]
    public void Evaluate_WhenOriginMatchesPark_ShouldReturnMaximumScore()
    {
        GeoPoint point = new GeoPoint(50.6292d, 3.0573d);

        ParkFitTravelEvaluation result = this.evaluator.Evaluate(
            point,
            point,
            EvaluatedAtUtc);

        Assert.Equal(100m, result.Subscore.Value);
        Assert.Equal(0d, result.Distance!.DistanceKilometers);
    }

    [Fact]
    public void Evaluate_WhenParkCoordinatesAreMissing_ShouldReturnUnknown()
    {
        ParkFitTravelEvaluation result = this.evaluator.Evaluate(
            new GeoPoint(50d, 3d),
            null,
            EvaluatedAtUtc);

        Assert.Equal(ParkFitSubscoreState.Unknown, result.Subscore.State);
        Assert.Null(result.Distance);
    }
}
