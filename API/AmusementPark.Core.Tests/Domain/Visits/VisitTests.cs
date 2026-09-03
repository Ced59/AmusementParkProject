using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class VisitTests
{
    private static readonly DateTime InitialUtc = new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeAPrivateDraftAndNormalizeMetadata()
    {
        Visit visit = CreateVisit(
            title: "  Première visite  ",
            privateNote: "  Souvenir privé  ",
            timeZoneId: "  Europe/Paris  ");

        Assert.Equal("visit-1", visit.Id.Value);
        Assert.Equal("user-1", visit.UserId);
        Assert.Equal("park-1", visit.ParkId);
        Assert.Equal(VisitStatus.Draft, visit.Status);
        Assert.Equal(VisitPrivacy.Private, visit.Privacy);
        Assert.Equal("Première visite", visit.Title);
        Assert.Equal("Souvenir privé", visit.PrivateNote);
        Assert.Equal("Europe/Paris", visit.TimeZoneId);
        Assert.Equal(LocalServiceDayConvention.VisitStartLocalDate, visit.ServiceDayConvention);
        Assert.Equal(1, visit.Version);
        Assert.Equal(InitialUtc, visit.CreatedAtUtc);
        Assert.Equal(InitialUtc, visit.UpdatedAtUtc);
        Assert.Null(visit.CompletedAtUtc);
    }

    [Fact]
    public void Create_WhenOptionalMetadataIsBlank_ShouldStoreNulls()
    {
        Visit visit = CreateVisit(title: " ", privateNote: "\t", timeZoneId: null);

        Assert.Null(visit.Title);
        Assert.Null(visit.PrivateNote);
        Assert.Null(visit.TimeZoneId);
    }

    [Fact]
    public void Create_ShouldAllowSeveralVisitsForTheSameParkAndDate()
    {
        Visit first = CreateVisit(id: "visit-1");
        Visit second = CreateVisit(id: "visit-2");

        Assert.Equal(first.UserId, second.UserId);
        Assert.Equal(first.ParkId, second.ParkId);
        Assert.Equal(first.Date, second.Date);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void UpdateDraft_ShouldChangeEditableStateAndIncrementVersionOnce()
    {
        Visit visit = CreateVisit();
        DateTime changedAtUtc = InitialUtc.AddMinutes(5);
        VisitDate newDate = VisitDate.ForMonth(2025, 8, true);

        visit.UpdateDraft(
            newDate,
            null,
            LocalServiceDayConvention.UserSelectedServiceDate,
            "  Été 2025  ",
            "  Date approximative  ",
            changedAtUtc);

        Assert.Equal(newDate, visit.Date);
        Assert.Null(visit.TimeZoneId);
        Assert.Equal(LocalServiceDayConvention.UserSelectedServiceDate, visit.ServiceDayConvention);
        Assert.Equal("Été 2025", visit.Title);
        Assert.Equal("Date approximative", visit.PrivateNote);
        Assert.Equal(2, visit.Version);
        Assert.Equal(changedAtUtc, visit.UpdatedAtUtc);
        Assert.Equal(InitialUtc, visit.CreatedAtUtc);
    }

    [Fact]
    public void UpdateDraft_WhenStateIsUnchanged_ShouldNotCreateAFakeRevision()
    {
        Visit visit = CreateVisit(title: "Visite", timeZoneId: "Europe/Paris");

        visit.UpdateDraft(
            VisitDate.ForDay(2026, 9, 3),
            " Europe/Paris ",
            LocalServiceDayConvention.VisitStartLocalDate,
            " Visite ",
            null,
            InitialUtc.AddMinutes(5));

        Assert.Equal(1, visit.Version);
        Assert.Equal(InitialUtc, visit.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateDraft_WhenStateIsUnchanged_ShouldStillValidateTheCommandTimestamp()
    {
        Visit visit = CreateVisit();

        VisitValidationException exception = Assert.Throws<VisitValidationException>(() => visit.UpdateDraft(
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            InitialUtc.AddMinutes(-1)));

        Assert.Equal(VisitErrorCodes.InvalidTimestampOrder, exception.ErrorCode);
        Assert.Equal(1, visit.Version);
    }

    [Fact]
    public void Complete_ShouldRecordCompletionAndIncrementVersion()
    {
        Visit visit = CreateVisit();
        DateTime completedAtUtc = InitialUtc.AddHours(1);

        visit.Complete(new DateOnly(2026, 9, 3), completedAtUtc);

        Assert.Equal(VisitStatus.Completed, visit.Status);
        Assert.Equal(completedAtUtc, visit.CompletedAtUtc);
        Assert.Equal(completedAtUtc, visit.UpdatedAtUtc);
        Assert.Equal(2, visit.Version);
    }

    [Theory]
    [MemberData(nameof(FutureVisitDates))]
    public void Complete_WhenDeclaredPeriodIsEntirelyInFuture_ShouldRejectIt(VisitDate date)
    {
        Visit visit = CreateVisit(date: date);

        VisitValidationException exception = Assert.Throws<VisitValidationException>(
            () => visit.Complete(new DateOnly(2026, 9, 3), InitialUtc.AddHours(1)));

        Assert.Equal(VisitErrorCodes.FutureCompletedDate, exception.ErrorCode);
        Assert.Equal(VisitStatus.Draft, visit.Status);
        Assert.Equal(1, visit.Version);
    }

    [Fact]
    public void Complete_WhenPartialPeriodIncludesToday_ShouldNotInventAFutureDay()
    {
        Visit visit = CreateVisit(date: VisitDate.ForMonth(2026, 9));

        visit.Complete(new DateOnly(2026, 9, 3), InitialUtc.AddHours(1));

        Assert.Equal(VisitStatus.Completed, visit.Status);
    }

    [Fact]
    public void Reopen_ShouldReturnCompletedVisitToDraft()
    {
        Visit visit = CreateVisit();
        visit.Complete(new DateOnly(2026, 9, 3), InitialUtc.AddHours(1));

        visit.Reopen(InitialUtc.AddHours(2));

        Assert.Equal(VisitStatus.Draft, visit.Status);
        Assert.Null(visit.CompletedAtUtc);
        Assert.Equal(3, visit.Version);
    }

    [Fact]
    public void Archive_WhenVisitWasCompleted_ShouldPreserveCompletionTimestamp()
    {
        Visit visit = CreateVisit();
        DateTime completedAtUtc = InitialUtc.AddHours(1);
        visit.Complete(new DateOnly(2026, 9, 3), completedAtUtc);

        visit.Archive(InitialUtc.AddHours(2));

        Assert.Equal(VisitStatus.Archived, visit.Status);
        Assert.Equal(completedAtUtc, visit.CompletedAtUtc);
        Assert.Equal(3, visit.Version);
    }

    [Fact]
    public void RestoreAsDraft_ShouldClearAnEarlierCompletionTimestamp()
    {
        Visit visit = CreateVisit();
        visit.Complete(new DateOnly(2026, 9, 3), InitialUtc.AddHours(1));
        visit.Archive(InitialUtc.AddHours(2));

        visit.RestoreAsDraft(InitialUtc.AddHours(3));

        Assert.Equal(VisitStatus.Draft, visit.Status);
        Assert.Null(visit.CompletedAtUtc);
        Assert.Equal(4, visit.Version);
    }

    [Fact]
    public void RestoreAsCompleted_ShouldPreserveAnEarlierCompletionTimestamp()
    {
        Visit visit = CreateVisit();
        DateTime completedAtUtc = InitialUtc.AddHours(1);
        visit.Complete(new DateOnly(2026, 9, 3), completedAtUtc);
        visit.Archive(InitialUtc.AddHours(2));

        visit.RestoreAsCompleted(new DateOnly(2026, 9, 3), InitialUtc.AddHours(3));

        Assert.Equal(VisitStatus.Completed, visit.Status);
        Assert.Equal(completedAtUtc, visit.CompletedAtUtc);
        Assert.Equal(4, visit.Version);
    }

    [Fact]
    public void RestoreAsCompleted_WhenArchivedDraftHadNoCompletion_ShouldUseMutationTime()
    {
        Visit visit = CreateVisit();
        visit.Archive(InitialUtc.AddHours(1));
        DateTime restoredAtUtc = InitialUtc.AddHours(2);

        visit.RestoreAsCompleted(new DateOnly(2026, 9, 3), restoredAtUtc);

        Assert.Equal(restoredAtUtc, visit.CompletedAtUtc);
    }

    [Theory]
    [InlineData("complete-twice")]
    [InlineData("reopen-draft")]
    [InlineData("archive-twice")]
    [InlineData("restore-active")]
    public void InvalidTransition_ShouldBeRejectedWithoutChangingVersion(string scenario)
    {
        Visit visit = CreateVisit();
        if (scenario == "complete-twice")
        {
            visit.Complete(new DateOnly(2026, 9, 3), InitialUtc.AddMinutes(1));
        }
        else if (scenario == "archive-twice")
        {
            visit.Archive(InitialUtc.AddMinutes(1));
        }

        long versionBeforeInvalidTransition = visit.Version;
        DateTime attemptedAtUtc = InitialUtc.AddMinutes(2);

        VisitValidationException exception = Assert.Throws<VisitValidationException>(() =>
        {
            if (scenario == "complete-twice")
            {
                visit.Complete(new DateOnly(2026, 9, 3), attemptedAtUtc);
            }
            else if (scenario == "reopen-draft")
            {
                visit.Reopen(attemptedAtUtc);
            }
            else if (scenario == "archive-twice")
            {
                visit.Archive(attemptedAtUtc);
            }
            else
            {
                visit.RestoreAsDraft(attemptedAtUtc);
            }
        });

        Assert.Equal(VisitErrorCodes.InvalidTransition, exception.ErrorCode);
        Assert.Equal(versionBeforeInvalidTransition, visit.Version);
    }

    [Fact]
    public void UpdateDraft_WhenVisitIsCompleted_ShouldRequireExplicitReopening()
    {
        Visit visit = CreateVisit();
        visit.Complete(new DateOnly(2026, 9, 3), InitialUtc.AddHours(1));

        VisitValidationException exception = Assert.Throws<VisitValidationException>(() => visit.UpdateDraft(
            VisitDate.ForDay(2026, 9, 2),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            InitialUtc.AddHours(2)));

        Assert.Equal(VisitErrorCodes.InvalidTransition, exception.ErrorCode);
    }

    [Theory]
    [InlineData(VisitPrivacy.Unlisted)]
    [InlineData(VisitPrivacy.Public)]
    [InlineData((VisitPrivacy)99)]
    public void Restore_WhenPrivacyIsNotPrivate_ShouldRejectIt(VisitPrivacy privacy)
    {
        VisitValidationException exception = Assert.Throws<VisitValidationException>(() => RestoreVisit(privacy: privacy));

        Assert.Equal(VisitErrorCodes.InvalidPrivacy, exception.ErrorCode);
    }

    [Fact]
    public void Restore_WhenCompletedTimestampDoesNotMatchStatus_ShouldRejectIt()
    {
        VisitValidationException missing = Assert.Throws<VisitValidationException>(() => RestoreVisit(
            status: VisitStatus.Completed,
            completedAtUtc: null));
        VisitValidationException unexpected = Assert.Throws<VisitValidationException>(() => RestoreVisit(
            status: VisitStatus.Draft,
            completedAtUtc: InitialUtc));

        Assert.Equal(VisitErrorCodes.CompletedAtRequired, missing.ErrorCode);
        Assert.Equal(VisitErrorCodes.CompletedAtForbidden, unexpected.ErrorCode);
    }

    [Fact]
    public void Restore_WhenVersionIsNotPositive_ShouldRejectIt()
    {
        VisitValidationException exception = Assert.Throws<VisitValidationException>(() => RestoreVisit(version: 0));

        Assert.Equal(VisitErrorCodes.InvalidVersion, exception.ErrorCode);
    }

    [Fact]
    public void Restore_WhenStatusOrServiceDayConventionIsUnknown_ShouldRejectIt()
    {
        VisitValidationException status = Assert.Throws<VisitValidationException>(() => RestoreVisit(
            status: (VisitStatus)99));
        VisitValidationException convention = Assert.Throws<VisitValidationException>(() => Visit.Restore(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            (LocalServiceDayConvention)99,
            VisitStatus.Draft,
            VisitPrivacy.Private,
            null,
            null,
            1,
            InitialUtc,
            InitialUtc,
            null));

        Assert.Equal(VisitErrorCodes.InvalidStatus, status.ErrorCode);
        Assert.Equal(VisitErrorCodes.InvalidServiceDayConvention, convention.ErrorCode);
    }

    [Fact]
    public void Restore_WhenTimestampsAreInvalid_ShouldRejectIt()
    {
        VisitValidationException nonUtc = Assert.Throws<VisitValidationException>(() => Visit.Restore(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            VisitStatus.Draft,
            VisitPrivacy.Private,
            null,
            null,
            1,
            DateTime.SpecifyKind(InitialUtc, DateTimeKind.Unspecified),
            InitialUtc,
            null));
        VisitValidationException reversed = Assert.Throws<VisitValidationException>(() => Visit.Restore(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            VisitStatus.Completed,
            VisitPrivacy.Private,
            null,
            null,
            1,
            InitialUtc,
            InitialUtc.AddMinutes(1),
            InitialUtc.AddMinutes(2)));

        Assert.Equal(VisitErrorCodes.TimestampNotUtc, nonUtc.ErrorCode);
        Assert.Equal(VisitErrorCodes.InvalidTimestampOrder, reversed.ErrorCode);
    }

    [Fact]
    public void Mutation_WhenTimestampIsNotUtcOrMovesBackward_ShouldRejectIt()
    {
        Visit visit = CreateVisit();
        VisitValidationException local = Assert.Throws<VisitValidationException>(
            () => visit.Archive(DateTime.SpecifyKind(InitialUtc.AddMinutes(1), DateTimeKind.Local)));
        VisitValidationException backward = Assert.Throws<VisitValidationException>(
            () => visit.Archive(InitialUtc.AddMinutes(-1)));

        Assert.Equal(VisitErrorCodes.TimestampNotUtc, local.ErrorCode);
        Assert.Equal(VisitErrorCodes.InvalidTimestampOrder, backward.ErrorCode);
        Assert.Equal(1, visit.Version);
    }

    [Fact]
    public void Create_WhenTextExceedsItsBound_ShouldRejectIt()
    {
        VisitValidationException title = Assert.Throws<VisitValidationException>(
            () => CreateVisit(title: new string('t', Visit.MaximumTitleLength + 1)));
        VisitValidationException note = Assert.Throws<VisitValidationException>(
            () => CreateVisit(privateNote: new string('n', Visit.MaximumPrivateNoteLength + 1)));
        VisitValidationException timeZone = Assert.Throws<VisitValidationException>(
            () => CreateVisit(timeZoneId: new string('z', Visit.MaximumTimeZoneIdLength + 1)));

        Assert.Equal(VisitErrorCodes.TitleTooLong, title.ErrorCode);
        Assert.Equal(VisitErrorCodes.PrivateNoteTooLong, note.ErrorCode);
        Assert.Equal(VisitErrorCodes.TimeZoneIdTooLong, timeZone.ErrorCode);
    }

    [Fact]
    public void Create_WhenTitleOrTimeZoneContainsControlCharacters_ShouldRejectIt()
    {
        VisitValidationException title = Assert.Throws<VisitValidationException>(
            () => CreateVisit(title: "Titre\nligne"));
        VisitValidationException timeZone = Assert.Throws<VisitValidationException>(
            () => CreateVisit(timeZoneId: "Europe/\nParis"));

        Assert.Equal(VisitErrorCodes.TitleControlCharacter, title.ErrorCode);
        Assert.Equal(VisitErrorCodes.TimeZoneIdControlCharacter, timeZone.ErrorCode);
    }

    [Theory]
    [InlineData("Titre\u2028ligne")]
    [InlineData("Titre\u2029paragraphe")]
    public void Create_WhenTitleContainsAUnicodeLineSeparator_ShouldRejectIt(string title)
    {
        VisitValidationException exception = Assert.Throws<VisitValidationException>(
            () => CreateVisit(title: title));

        Assert.Equal(VisitErrorCodes.TitleControlCharacter, exception.ErrorCode);
    }

    [Fact]
    public void Create_WhenPrivateNoteContainsLineBreaks_ShouldPreserveThem()
    {
        Visit visit = CreateVisit(privateNote: "Premier tour\nDeuxième tour");

        Assert.Equal("Premier tour\nDeuxième tour", visit.PrivateNote);
    }

    [Fact]
    public void Mutation_WhenVersionCannotBeIncremented_ShouldRejectIt()
    {
        Visit visit = RestoreVisit(version: long.MaxValue);

        VisitValidationException exception = Assert.Throws<VisitValidationException>(
            () => visit.Archive(InitialUtc.AddMinutes(1)));

        Assert.Equal(VisitErrorCodes.InvalidVersion, exception.ErrorCode);
        Assert.Equal(long.MaxValue, visit.Version);
    }

    public static IEnumerable<object[]> FutureVisitDates()
    {
        yield return new object[] { VisitDate.ForYear(2027) };
        yield return new object[] { VisitDate.ForMonth(2026, 10) };
        yield return new object[] { VisitDate.ForDay(2026, 9, 4) };
    }

    private static Visit CreateVisit(
        string id = "visit-1",
        VisitDate? date = null,
        string? title = null,
        string? privateNote = null,
        string? timeZoneId = "Europe/Paris")
    {
        return Visit.Create(
            VisitId.Parse(id),
            " user-1 ",
            " park-1 ",
            date ?? VisitDate.ForDay(2026, 9, 3),
            timeZoneId,
            LocalServiceDayConvention.VisitStartLocalDate,
            title,
            privateNote,
            InitialUtc);
    }

    private static Visit RestoreVisit(
        VisitStatus status = VisitStatus.Draft,
        VisitPrivacy privacy = VisitPrivacy.Private,
        long version = 3,
        DateTime? completedAtUtc = null)
    {
        return Visit.Restore(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            status,
            privacy,
            null,
            null,
            version,
            InitialUtc,
            InitialUtc,
            completedAtUtc);
    }
}
