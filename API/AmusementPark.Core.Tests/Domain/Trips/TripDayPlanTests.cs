using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripDayPlanTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldOrderBlocksAndNormalizeTheGroupNote()
    {
        TripDayBlock later = new(
            TripDayBlockId.New(),
            TripDayBlockType.Event,
            "Spectacle",
            null,
            new TimeOnly(16, 0),
            2048);
        TripDayBlock earlier = new(
            TripDayBlockId.New(),
            TripDayBlockType.Meal,
            "Déjeuner",
            null,
            new TimeOnly(12, 0),
            1024);

        TripDayPlan day = CreateDay(new[] { later, earlier }, "  Rendez-vous à l'entrée  ");

        Assert.Equal(new[] { earlier.Id, later.Id }, day.Blocks.Select(static block => block.Id));
        Assert.Equal("Rendez-vous à l'entrée", day.GroupNote);
        Assert.Equal(1, day.Version);
    }

    [Fact]
    public void Update_ShouldRejectDuplicateBlockPositions()
    {
        TripDayPlan day = CreateDay(Array.Empty<TripDayBlock>(), null);
        TripDayBlock first = new(
            TripDayBlockId.New(),
            TripDayBlockType.Note,
            "Premier",
            null,
            null,
            1024);
        TripDayBlock second = new(
            TripDayBlockId.New(),
            TripDayBlockType.Note,
            "Second",
            null,
            null,
            1024);

        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() => day.Update(
            day.ParkCandidateId,
            day.ParkId,
            null,
            null,
            new[] { first, second },
            CreatedAtUtc.AddMinutes(1)));

        Assert.Equal(TripPlanErrorCodes.InvalidDayPlan, exception.Code);
    }

    [Fact]
    public void Update_ShouldCompareBlockFieldsStructurallyWhenTextContainsSeparators()
    {
        TripDayBlockId blockId = TripDayBlockId.New();
        TripDayBlock original = new(
            blockId,
            TripDayBlockType.Note,
            "a|b",
            "c",
            null,
            1024);
        TripDayPlan day = CreateDay(new[] { original }, null);
        TripDayBlock changed = new(
            blockId,
            TripDayBlockType.Note,
            "a",
            "b|c",
            null,
            1024);

        day.Update(
            day.ParkCandidateId,
            day.ParkId,
            day.DesiredArrivalTime,
            day.GroupNote,
            new[] { changed },
            CreatedAtUtc.AddMinutes(1));

        Assert.Equal(2, day.Version);
        Assert.Equal("a", Assert.Single(day.Blocks).Title);
    }

    private static TripDayPlan CreateDay(
        IReadOnlyCollection<TripDayBlock> blocks,
        string? note)
    {
        return TripDayPlan.Create(
            TripDayPlanId.New(),
            TripPlanId.New(),
            new DateOnly(2027, 7, 8),
            TripParkCandidateId.New(),
            "park-1",
            new TimeOnly(9, 30),
            note,
            blocks,
            CreatedAtUtc);
    }
}
