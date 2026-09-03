using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class RideAssessmentTests
{
    private static readonly DateTime CreatedAtUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UpsertAssessment_ShouldCreateOnePrivateTemporalAssessment()
    {
        RideOccurrence occurrence = CreateOccurrence();
        DateTime assessedAtUtc = CreatedAtUtc.AddHours(2);

        occurrence.UpsertAssessment(
            RatingValue.FromDouble(4.5d),
            "  Premier rang mémorable  ",
            assessedAtUtc);

        Assert.NotNull(occurrence.Assessment);
        Assert.Equal(9, occurrence.Assessment.Value.HalfSteps);
        Assert.Equal("Premier rang mémorable", occurrence.Assessment.PrivateComment);
        Assert.Equal(1, occurrence.Assessment.Revision);
        Assert.Equal(assessedAtUtc, occurrence.Assessment.CreatedAtUtc);
        Assert.Equal(2, occurrence.Version);
        Assert.Equal(assessedAtUtc, occurrence.UpdatedAtUtc);
    }

    [Fact]
    public void UpsertAssessment_WhenOneExists_ShouldReplaceItAndIncrementRevisions()
    {
        RideOccurrence occurrence = CreateOccurrence();
        occurrence.UpsertAssessment(
            RatingValue.FromDouble(4d),
            "Première impression",
            CreatedAtUtc.AddHours(1));

        occurrence.UpsertAssessment(
            RatingValue.FromDouble(3.5d),
            null,
            CreatedAtUtc.AddHours(2));

        Assert.Equal(3.5d, occurrence.Assessment?.Value.DoubleValue);
        Assert.Null(occurrence.Assessment?.PrivateComment);
        Assert.Equal(2, occurrence.Assessment?.Revision);
        Assert.Equal(CreatedAtUtc.AddHours(1), occurrence.Assessment?.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc.AddHours(2), occurrence.Assessment?.UpdatedAtUtc);
        Assert.Equal(3, occurrence.Version);
    }

    [Fact]
    public void DeleteAssessment_ShouldRemoveItAndIncrementTheOccurrenceVersion()
    {
        RideOccurrence occurrence = CreateOccurrence();
        occurrence.UpsertAssessment(
            RatingValue.FromDouble(3.5d),
            null,
            CreatedAtUtc.AddHours(1));

        occurrence.DeleteAssessment(CreatedAtUtc.AddHours(2));

        Assert.Null(occurrence.Assessment);
        Assert.Equal(3, occurrence.Version);
        Assert.Equal(CreatedAtUtc.AddHours(2), occurrence.UpdatedAtUtc);
    }

    [Fact]
    public void DeleteAssessment_WhenNoneExists_ShouldBeANoOp()
    {
        RideOccurrence occurrence = CreateOccurrence();

        occurrence.DeleteAssessment(CreatedAtUtc.AddHours(1));

        Assert.Null(occurrence.Assessment);
        Assert.Equal(1, occurrence.Version);
        Assert.Equal(CreatedAtUtc, occurrence.UpdatedAtUtc);
    }

    [Fact]
    public void UpsertAssessment_WhenCommentIsTooLong_ShouldLeaveOccurrenceUnchanged()
    {
        RideOccurrence occurrence = CreateOccurrence();

        RideAssessmentValidationException exception =
            Assert.Throws<RideAssessmentValidationException>(
                () => occurrence.UpsertAssessment(
                    RatingValue.FromDouble(4d),
                    new string('a', RideAssessment.MaximumPrivateCommentLength + 1),
                    CreatedAtUtc.AddHours(1)));

        Assert.Equal(RideAssessmentErrorCodes.PrivateCommentTooLong, exception.ErrorCode);
        Assert.Null(occurrence.Assessment);
        Assert.Equal(1, occurrence.Version);
    }

    [Fact]
    public void Restore_WhenAssessmentFallsOutsideOccurrenceLifetime_ShouldRejectIt()
    {
        RideAssessment assessment = RideAssessment.Restore(
            RatingValue.FromDouble(4d),
            null,
            1,
            CreatedAtUtc,
            CreatedAtUtc.AddHours(2));

        RideOccurrenceValidationException exception = Assert.Throws<RideOccurrenceValidationException>(
            () => RideOccurrence.Restore(
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
                CreatedAtUtc,
                CreatedAtUtc.AddHours(1),
                null,
                assessment));

        Assert.Equal(RideOccurrenceErrorCodes.InvalidTimestampOrder, exception.ErrorCode);
    }

    [Fact]
    public void UpsertAssessment_WhenOccurrenceIsDeleted_ShouldRejectIt()
    {
        RideOccurrence occurrence = CreateOccurrence();
        occurrence.Delete(CreatedAtUtc.AddHours(1));

        RideOccurrenceValidationException exception = Assert.Throws<RideOccurrenceValidationException>(
            () => occurrence.UpsertAssessment(
                RatingValue.FromDouble(4d),
                null,
                CreatedAtUtc.AddHours(2)));

        Assert.Equal(RideOccurrenceErrorCodes.DeletedOccurrenceMutation, exception.ErrorCode);
    }

    private static RideOccurrence CreateOccurrence()
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            CreatedAtUtc);
        return RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-1"),
            visit,
            "item-1",
            1024,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            CreatedAtUtc);
    }
}
