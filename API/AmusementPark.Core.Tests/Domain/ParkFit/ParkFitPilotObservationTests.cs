using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitPilotObservationTests
{
    [Fact]
    public void CreateCompleted_ShouldKeepOnlyDistinctBoundedAggregateDimensions()
    {
        ParkFitPilotObservation observation = ParkFitPilotObservation.Create(
            ParkFitPilotEventKind.SearchCompleted,
            ParkFitPilotResultBand.TwoToFour,
            ParkFitPilotUnknownLevel.Limited,
            ParkFitPilotDurationBand.UnderOneAndHalfSeconds,
            null,
            null,
            $" {ParkFitScoreEvaluator.MethodVersion} ",
            [ParkFitDataQualityIssue.StaleEvidence, ParkFitDataQualityIssue.StaleEvidence]);

        Assert.Equal(ParkFitScoreEvaluator.MethodVersion, observation.MethodVersion);
        Assert.Equal(ParkFitDataQualityIssue.StaleEvidence, Assert.Single(observation.QualityIssues));
    }

    [Fact]
    public void CreateStarted_WithSearchDetails_ShouldRejectInconsistentPayload()
    {
        Assert.Throws<ArgumentException>(() => ParkFitPilotObservation.Create(
            ParkFitPilotEventKind.SearchStarted,
            ParkFitPilotResultBand.One,
            null,
            null,
            null,
            null,
            null,
            []));
    }

    [Fact]
    public void CreateCompleted_WithUnknownMethodVersion_ShouldRejectUnboundedCounterKey()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ParkFitPilotObservation.Create(
            ParkFitPilotEventKind.SearchCompleted,
            ParkFitPilotResultBand.One,
            ParkFitPilotUnknownLevel.None,
            ParkFitPilotDurationBand.UnderHalfSecond,
            null,
            null,
            "caller-controlled-version",
            []));
    }
}
