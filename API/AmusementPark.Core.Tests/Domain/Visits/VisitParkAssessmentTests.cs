using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class VisitParkAssessmentTests
{
    private static readonly DateTime CreatedAtUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UpsertParkAssessment_ShouldCreateOnePrivateTemporalAssessment()
    {
        Visit visit = CreateVisit();
        DateTime assessedAtUtc = CreatedAtUtc.AddHours(2);

        visit.UpsertParkAssessment(
            RatingValue.FromDouble(4.5d),
            "  Très bonne journée  ",
            assessedAtUtc);

        Assert.NotNull(visit.ParkAssessment);
        Assert.Equal(9, visit.ParkAssessment.Value.HalfSteps);
        Assert.Equal(4.5d, visit.ParkAssessment.Value.DoubleValue);
        Assert.Equal("Très bonne journée", visit.ParkAssessment.PrivateComment);
        Assert.Equal(1, visit.ParkAssessment.Revision);
        Assert.Equal(assessedAtUtc, visit.ParkAssessment.CreatedAtUtc);
        Assert.Equal(assessedAtUtc, visit.ParkAssessment.UpdatedAtUtc);
        Assert.Equal(2, visit.Version);
        Assert.Equal(assessedAtUtc, visit.UpdatedAtUtc);
    }

    [Fact]
    public void UpsertParkAssessment_WhenOneExists_ShouldReplaceItAndIncrementRevisions()
    {
        Visit visit = CreateVisit();
        visit.UpsertParkAssessment(
            RatingValue.FromDouble(4d),
            "Première impression",
            CreatedAtUtc.AddHours(1));

        visit.UpsertParkAssessment(
            RatingValue.FromDouble(4.5d),
            null,
            CreatedAtUtc.AddHours(2));

        Assert.NotNull(visit.ParkAssessment);
        Assert.Equal(4.5d, visit.ParkAssessment.Value.DoubleValue);
        Assert.Null(visit.ParkAssessment.PrivateComment);
        Assert.Equal(2, visit.ParkAssessment.Revision);
        Assert.Equal(CreatedAtUtc.AddHours(1), visit.ParkAssessment.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc.AddHours(2), visit.ParkAssessment.UpdatedAtUtc);
        Assert.Equal(3, visit.Version);
    }

    [Fact]
    public void DeleteParkAssessment_ShouldRemoveItAndIncrementTheVisitVersion()
    {
        Visit visit = CreateVisit();
        visit.UpsertParkAssessment(
            RatingValue.FromDouble(3.5d),
            null,
            CreatedAtUtc.AddHours(1));

        visit.DeleteParkAssessment(CreatedAtUtc.AddHours(2));

        Assert.Null(visit.ParkAssessment);
        Assert.Equal(3, visit.Version);
        Assert.Equal(CreatedAtUtc.AddHours(2), visit.UpdatedAtUtc);
    }

    [Fact]
    public void DeleteParkAssessment_WhenNoneExists_ShouldBeANoOp()
    {
        Visit visit = CreateVisit();

        visit.DeleteParkAssessment(CreatedAtUtc.AddHours(1));

        Assert.Null(visit.ParkAssessment);
        Assert.Equal(1, visit.Version);
        Assert.Equal(CreatedAtUtc, visit.UpdatedAtUtc);
    }

    [Fact]
    public void UpsertParkAssessment_WhenCommentIsTooLong_ShouldLeaveVisitUnchanged()
    {
        Visit visit = CreateVisit();

        VisitParkAssessmentValidationException exception =
            Assert.Throws<VisitParkAssessmentValidationException>(
                () => visit.UpsertParkAssessment(
                    RatingValue.FromDouble(4d),
                    new string('a', VisitParkAssessment.MaximumPrivateCommentLength + 1),
                    CreatedAtUtc.AddHours(1)));

        Assert.Equal(
            VisitParkAssessmentErrorCodes.PrivateCommentTooLong,
            exception.ErrorCode);
        Assert.Null(visit.ParkAssessment);
        Assert.Equal(1, visit.Version);
    }

    [Fact]
    public void Restore_WhenAssessmentFallsOutsideVisitLifetime_ShouldRejectIt()
    {
        VisitParkAssessment assessment = VisitParkAssessment.Restore(
            RatingValue.FromDouble(4d),
            null,
            1,
            CreatedAtUtc,
            CreatedAtUtc.AddHours(2));

        VisitValidationException exception = Assert.Throws<VisitValidationException>(
            () => Visit.Restore(
                VisitId.Parse("visit-1"),
                "user-1",
                "park-1",
                VisitDate.ForDay(2026, 9, 3),
                "Europe/Paris",
                LocalServiceDayConvention.VisitStartLocalDate,
                VisitStatus.Draft,
                VisitPrivacy.Private,
                null,
                null,
                2,
                CreatedAtUtc,
                CreatedAtUtc.AddHours(1),
                null,
                assessment));

        Assert.Equal(VisitErrorCodes.InvalidTimestampOrder, exception.ErrorCode);
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            CreatedAtUtc);
    }
}
