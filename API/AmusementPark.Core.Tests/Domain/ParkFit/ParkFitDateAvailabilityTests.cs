using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitDateAvailabilityTests
{
    [Fact]
    public void Constructor_ShouldPreserveStateAndEvaluationDate()
    {
        DateOnly evaluationDate = new DateOnly(2026, 9, 14);

        ParkFitDateAvailability result = new ParkFitDateAvailability(
            ParkFitDateAvailabilityState.Available,
            evaluationDate);

        Assert.Equal(ParkFitDateAvailabilityState.Available, result.State);
        Assert.Equal(evaluationDate, result.EvaluationDate);
    }

    [Fact]
    public void Constructor_WhenStateIsInvalid_ShouldRejectIt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParkFitDateAvailability(
            (ParkFitDateAvailabilityState)99,
            new DateOnly(2026, 9, 14)));
    }
}
