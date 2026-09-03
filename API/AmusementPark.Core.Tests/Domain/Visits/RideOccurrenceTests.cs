using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class RideOccurrenceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithAnExactVisitAndLocalTime_ShouldPreserveTheDeclaration()
    {
        Visit visit = CreateVisit(VisitDate.ForDay(2026, 9, 3), "Europe/Paris");
        OccurrenceMoment moment = new OccurrenceMoment(new TimeOnly(14, 35), true);

        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-1"),
            visit,
            "item-1",
            RideOccurrence.SortPositionStep,
            moment,
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            "  Premier rang  ",
            NowUtc);

        Assert.Equal(visit.Id, occurrence.VisitId);
        Assert.Equal("user-1", occurrence.UserId);
        Assert.Equal("park-1", occurrence.ParkId);
        Assert.Equal("item-1", occurrence.ParkItemId);
        Assert.Equal(moment, occurrence.Moment);
        Assert.Equal("Premier rang", occurrence.PrivateNote);
        Assert.True(occurrence.CountsAsRide);
        Assert.False(occurrence.IsDeleted);
        Assert.Equal(1, occurrence.Version);
    }

    [Theory]
    [InlineData(VisitDatePrecision.Year, null)]
    [InlineData(VisitDatePrecision.Month, "Europe/Paris")]
    [InlineData(VisitDatePrecision.Day, null)]
    public void Create_WithLocalTimeButIncompleteTemporalContext_ShouldFail(
        VisitDatePrecision precision,
        string? timeZoneId)
    {
        VisitDate date = precision switch
        {
            VisitDatePrecision.Year => VisitDate.ForYear(2026),
            VisitDatePrecision.Month => VisitDate.ForMonth(2026, 9),
            VisitDatePrecision.Day => VisitDate.ForDay(2026, 9, 3),
            _ => throw new InvalidOperationException(),
        };
        Visit visit = CreateVisit(date, timeZoneId);

        RideOccurrenceValidationException exception =
            Assert.Throws<RideOccurrenceValidationException>(() => RideOccurrence.Create(
                RideOccurrenceId.Parse("occurrence-1"),
                visit,
                "item-1",
                1024,
                new OccurrenceMoment(new TimeOnly(12, 0), false),
                RideOccurrenceStatus.Completed,
                RideLogSource.Manual,
                HistoricalConsistency.Unverified,
                null,
                null,
                NowUtc));

        Assert.Equal(
            RideOccurrenceErrorCodes.TimeRequiresExactDayAndTimeZone,
            exception.ErrorCode);
    }

    [Fact]
    public void Create_WithoutLocalTime_ShouldAllowAnApproximatePartialVisit()
    {
        Visit visit = CreateVisit(VisitDate.ForYear(1998, true), null);

        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-1"),
            visit,
            "item-1",
            1024,
            new OccurrenceMoment(null, true),
            RideOccurrenceStatus.MissedClosed,
            RideLogSource.Manual,
            HistoricalConsistency.Unverified,
            null,
            null,
            NowUtc);

        Assert.Null(occurrence.Moment.LocalTime);
        Assert.True(occurrence.Moment.IsApproximate);
        Assert.False(occurrence.CountsAsRide);
    }

    [Theory]
    [InlineData(RideOccurrenceStatus.Completed, true)]
    [InlineData(RideOccurrenceStatus.Attempted, false)]
    [InlineData(RideOccurrenceStatus.MissedClosed, false)]
    [InlineData(RideOccurrenceStatus.MissedUnavailable, false)]
    [InlineData(RideOccurrenceStatus.SkippedByChoice, false)]
    public void CountsAsRide_ShouldOnlyCountCompletedOccurrences(
        RideOccurrenceStatus status,
        bool expected)
    {
        RideOccurrence occurrence = CreateOccurrence(status: status);

        Assert.Equal(expected, occurrence.CountsAsRide);
    }

    [Fact]
    public void Update_ShouldVersionARealChangeButNotANoOp()
    {
        Visit visit = CreateVisit(VisitDate.ForDay(2026, 9, 3), "Europe/Paris");
        RideOccurrence occurrence = CreateOccurrence(visit: visit);

        occurrence.Update(
            visit,
            occurrence.Moment,
            occurrence.Status,
            occurrence.HistoricalConsistency,
            occurrence.HistoricalTarget,
            occurrence.PrivateNote,
            NowUtc.AddMinutes(1));
        Assert.Equal(1, occurrence.Version);

        occurrence.Update(
            visit,
            new OccurrenceMoment(new TimeOnly(15, 0), false),
            RideOccurrenceStatus.Attempted,
            HistoricalConsistency.Unverified,
            new HistoricalTargetReference("Ancien nom", "Montagnes russes"),
            "Correction",
            NowUtc.AddMinutes(2));

        Assert.Equal(2, occurrence.Version);
        Assert.Equal(NowUtc.AddMinutes(2), occurrence.UpdatedAtUtc);
        Assert.Equal(RideOccurrenceStatus.Attempted, occurrence.Status);
        Assert.False(occurrence.CountsAsRide);
        Assert.Equal("Ancien nom", occurrence.HistoricalTarget?.Name);
    }

    [Fact]
    public void Update_WithAnotherVisitScope_ShouldFail()
    {
        RideOccurrence occurrence = CreateOccurrence();
        Visit anotherVisit = Visit.Create(
            VisitId.Parse("visit-2"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);

        RideOccurrenceValidationException exception =
            Assert.Throws<RideOccurrenceValidationException>(() => occurrence.Update(
                anotherVisit,
                occurrence.Moment,
                occurrence.Status,
                occurrence.HistoricalConsistency,
                null,
                null,
                NowUtc.AddMinutes(1)));

        Assert.Equal(RideOccurrenceErrorCodes.VisitScopeMismatch, exception.ErrorCode);
    }

    [Fact]
    public void MoveTo_ShouldAcceptTheLongOrderingRangeAndIncrementVersion()
    {
        RideOccurrence occurrence = CreateOccurrence();

        occurrence.MoveTo(long.MinValue, NowUtc.AddMinutes(1));

        Assert.Equal(long.MinValue, occurrence.SortPosition);
        Assert.Equal(2, occurrence.Version);
    }

    [Fact]
    public void Delete_ShouldCreateATombstoneAndBlockLaterMutations()
    {
        RideOccurrence occurrence = CreateOccurrence();

        occurrence.Delete(NowUtc.AddMinutes(1));

        Assert.True(occurrence.IsDeleted);
        Assert.Equal(NowUtc.AddMinutes(1), occurrence.DeletedAtUtc);
        Assert.Equal(2, occurrence.Version);
        RideOccurrenceValidationException exception =
            Assert.Throws<RideOccurrenceValidationException>(
                () => occurrence.MoveTo(2048, NowUtc.AddMinutes(2)));
        Assert.Equal(RideOccurrenceErrorCodes.DeletedOccurrenceMutation, exception.ErrorCode);
    }

    [Fact]
    public void Restore_WithInconsistentTimestamps_ShouldFail()
    {
        RideOccurrenceValidationException exception =
            Assert.Throws<RideOccurrenceValidationException>(() => RideOccurrence.Restore(
                RideOccurrenceId.Parse("occurrence-1"),
                VisitId.Parse("visit-1"),
                "user-1",
                "park-1",
                "item-1",
                1024,
                new OccurrenceMoment(null, false),
                RideOccurrenceStatus.Completed,
                RideLogSource.Manual,
                HistoricalConsistency.Verified,
                null,
                null,
                2,
                NowUtc,
                NowUtc.AddMinutes(1),
                NowUtc.AddMinutes(2)));

        Assert.Equal(RideOccurrenceErrorCodes.InvalidTimestampOrder, exception.ErrorCode);
    }

    [Fact]
    public void HistoricalTargetReference_ShouldNormalizeAndRejectControlCharacters()
    {
        HistoricalTargetReference reference = new HistoricalTargetReference(
            "  Ancien nom  ",
            "  Attraction  ");

        Assert.Equal("Ancien nom", reference.Name);
        Assert.Equal("Attraction", reference.Category);
        RideOccurrenceValidationException exception =
            Assert.Throws<RideOccurrenceValidationException>(
                () => new HistoricalTargetReference("Nom\ninterdit", null));
        Assert.Equal(
            RideOccurrenceErrorCodes.HistoricalTargetControlCharacter,
            exception.ErrorCode);
    }

    private static RideOccurrence CreateOccurrence(
        Visit? visit = null,
        RideOccurrenceStatus status = RideOccurrenceStatus.Completed)
    {
        Visit selectedVisit = visit
            ?? CreateVisit(VisitDate.ForDay(2026, 9, 3), "Europe/Paris");
        return RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-1"),
            selectedVisit,
            "item-1",
            1024,
            new OccurrenceMoment(null, false),
            status,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            NowUtc);
    }

    private static Visit CreateVisit(VisitDate date, string? timeZoneId)
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            date,
            timeZoneId,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);
    }
}
