using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class PassportAuditEventFactoryTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 4, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ParkAssessmentUpserted_ShouldKeepRatingsButOnlySignalPrivateTextChanges()
    {
        Visit visit = CreateVisit();
        visit.UpsertParkAssessment(
            RatingValue.FromDouble(4.5d),
            "Un commentaire qui ne doit jamais être journalisé",
            NowUtc.AddMinutes(1));

        PassportAuditEvent auditEvent =
            PassportVisitAuditEventFactory.ParkAssessmentUpserted(visit, null);

        Assert.Equal(PassportAuditEventType.ParkAssessmentCreated, auditEvent.EventType);
        Assert.Null(auditEvent.PreviousRatingHalfSteps);
        Assert.Equal((byte)9, auditEvent.NewRatingHalfSteps);
        Assert.True(auditEvent.PrivateTextChanged);
        Assert.Contains(
            PassportAuditChangedField.ParkAssessmentPrivateComment,
            auditEvent.ChangedFields);
        Assert.DoesNotContain(
            auditEvent.GetType().GetProperties(),
            property => property.Name.Contains("Comment", StringComparison.Ordinal));
    }

    [Fact]
    public void RideOccurrenceChanged_ShouldMinimizeMomentAndPrivateNoteValues()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit);
        RideOccurrenceAuditSnapshot previous =
            RideOccurrenceAuditSnapshot.Capture(occurrence);
        occurrence.Update(
            visit,
            new OccurrenceMoment(new TimeOnly(10, 30), true),
            RideOccurrenceStatus.Attempted,
            HistoricalConsistency.Verified,
            null,
            "Texte privé corrigé",
            NowUtc.AddMinutes(1));

        PassportAuditEvent auditEvent = PassportRideAuditEventFactory.RideOccurrenceChanged(
            occurrence,
            previous,
            "operation-1");

        Assert.Equal(RideOccurrenceStatus.Completed, auditEvent.PreviousRideStatus);
        Assert.Equal(RideOccurrenceStatus.Attempted, auditEvent.NewRideStatus);
        Assert.Contains(PassportAuditChangedField.Moment, auditEvent.ChangedFields);
        Assert.Contains(PassportAuditChangedField.PrivateNote, auditEvent.ChangedFields);
        Assert.True(auditEvent.PrivateTextChanged);
        Assert.DoesNotContain(
            auditEvent.GetType().GetProperties(),
            property => property.PropertyType == typeof(TimeOnly)
                || property.PropertyType == typeof(OccurrenceMoment));
    }

    [Fact]
    public void RideAssessmentUpserted_WhenSameValuesAreSaved_ShouldAuditTheRevision()
    {
        Visit visit = CreateVisit();
        RideOccurrence occurrence = CreateOccurrence(visit);
        occurrence.UpsertAssessment(
            RatingValue.FromDouble(4d),
            "Privé",
            NowUtc.AddMinutes(1));
        RideOccurrenceAuditSnapshot previous =
            RideOccurrenceAuditSnapshot.Capture(occurrence);
        occurrence.UpsertAssessment(
            RatingValue.FromDouble(4d),
            "Privé",
            NowUtc.AddMinutes(2));

        PassportAuditEvent auditEvent =
            PassportRideAuditEventFactory.RideAssessmentUpserted(occurrence, previous);

        Assert.Equal(PassportAuditEventType.RideAssessmentChanged, auditEvent.EventType);
        Assert.Contains(PassportAuditChangedField.AssessmentRevision, auditEvent.ChangedFields);
        Assert.False(auditEvent.PrivateTextChanged);
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 4),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);
    }

    private static RideOccurrence CreateOccurrence(Visit visit)
    {
        return RideOccurrence.Create(
            RideOccurrenceId.Parse("ride-1"),
            visit,
            "item-1",
            1024,
            new OccurrenceMoment(new TimeOnly(10, 0), false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            "Texte privé initial",
            NowUtc);
    }
}
