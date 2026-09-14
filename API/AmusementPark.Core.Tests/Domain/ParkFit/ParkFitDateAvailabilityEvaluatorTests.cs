using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitDateAvailabilityEvaluatorTests
{
    private static readonly DateOnly EvaluationDate = new DateOnly(2026, 9, 14);
    private readonly ParkFitDateAvailabilityEvaluator evaluator =
        new ParkFitDateAvailabilityEvaluator();

    [Fact]
    public void Evaluate_WhenNoScheduleExists_ShouldReturnUnknown()
    {
        ParkFitDateAvailability result = this.evaluator.Evaluate(null, EvaluationDate);

        Assert.Equal(ParkFitDateAvailabilityState.Unknown, result.State);
        Assert.Equal(EvaluationDate, result.EvaluationDate);
    }

    [Theory]
    [InlineData(false, ParkFitDateAvailabilityState.Available)]
    [InlineData(true, ParkFitDateAvailabilityState.Unavailable)]
    public void Evaluate_WhenDateIsDefined_ShouldReturnItsExactState(
        bool isClosed,
        ParkFitDateAvailabilityState expectedState)
    {
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = "park-1",
            RegularRules = new List<ParkOpeningHoursRule>
            {
                new ParkOpeningHoursRule
                {
                    StartDate = EvaluationDate,
                    EndDate = EvaluationDate,
                    DaysOfWeek = new List<DayOfWeek> { EvaluationDate.DayOfWeek },
                    IsClosed = isClosed,
                    TimeRanges = isClosed
                        ? new List<ParkOpeningHoursTimeRange>()
                        : new List<ParkOpeningHoursTimeRange>
                        {
                            new ParkOpeningHoursTimeRange
                            {
                                OpensAt = new TimeOnly(10, 0),
                                ClosesAt = new TimeOnly(18, 0),
                            },
                        },
                },
            },
        };

        ParkFitDateAvailability result = this.evaluator.Evaluate(
            schedule,
            EvaluationDate);

        Assert.Equal(expectedState, result.State);
    }

    [Fact]
    public void Evaluate_WhenDateIsOutsideSchedule_ShouldReturnUnknown()
    {
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = "park-1",
        };

        ParkFitDateAvailability result = this.evaluator.Evaluate(
            schedule,
            EvaluationDate);

        Assert.Equal(ParkFitDateAvailabilityState.Unknown, result.State);
    }
}
