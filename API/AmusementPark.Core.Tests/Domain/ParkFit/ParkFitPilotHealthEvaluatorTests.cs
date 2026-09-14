using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitPilotHealthEvaluatorTests
{
    [Fact]
    public void Evaluate_WithFrequentEmptyResults_ShouldRequestAttention()
    {
        ParkFitPilotHealth health = ParkFitPilotHealthEvaluator.Evaluate(20, 16, 5, 1, 4, 2);

        Assert.Equal(80m, health.CompletionRatePercent);
        Assert.Equal(31.3m, health.NoResultRatePercent);
        Assert.Equal(ParkFitPilotSignal.NeedsAttention, health.Signal);
        Assert.True(health.RequiresQualitativeReview);
    }

    [Fact]
    public void Evaluate_WithoutCompletedSearch_ShouldAwaitObservations()
    {
        ParkFitPilotHealth health = ParkFitPilotHealthEvaluator.Evaluate(0, 0, 0, 0, 0, 0);

        Assert.Equal(ParkFitPilotSignal.AwaitingObservations, health.Signal);
        Assert.Equal(0m, health.CompletionRatePercent);
    }
}
