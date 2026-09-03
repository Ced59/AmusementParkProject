using System.Globalization;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Session privée déclarée par un utilisateur dans un parc.
/// </summary>
public sealed class Visit
{
    public const int MaximumTitleLength = 160;

    public const int MaximumPrivateNoteLength = 4000;

    public const int MaximumTimeZoneIdLength = 128;

    private Visit(
        VisitId id,
        string userId,
        string parkId,
        VisitDate date,
        string? timeZoneId,
        LocalServiceDayConvention serviceDayConvention,
        VisitStatus status,
        VisitPrivacy privacy,
        string? title,
        string? privateNote,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? completedAtUtc)
    {
        _ = id.Value;
        ArgumentNullException.ThrowIfNull(date);
        ValidateServiceDayConvention(serviceDayConvention);
        ValidateStatus(status);
        ValidatePrivacy(privacy);
        ValidateVersion(version);
        ValidateRestoredTimestamps(status, createdAtUtc, updatedAtUtc, completedAtUtc);

        this.Id = id;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.Date = date;
        this.TimeZoneId = NormalizeTimeZoneId(timeZoneId);
        this.ServiceDayConvention = serviceDayConvention;
        this.Status = status;
        this.Privacy = privacy;
        this.Title = NormalizeTitle(title);
        this.PrivateNote = NormalizePrivateNote(privateNote);
        this.Version = version;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.CompletedAtUtc = completedAtUtc;
    }

    public VisitId Id { get; }

    public string UserId { get; }

    public string ParkId { get; }

    public VisitDate Date { get; private set; }

    public string? TimeZoneId { get; private set; }

    public LocalServiceDayConvention ServiceDayConvention { get; private set; }

    public VisitStatus Status { get; private set; }

    public VisitPrivacy Privacy { get; }

    public string? Title { get; private set; }

    public string? PrivateNote { get; private set; }

    public long Version { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public static Visit Create(
        VisitId id,
        string userId,
        string parkId,
        VisitDate date,
        string? timeZoneId,
        LocalServiceDayConvention serviceDayConvention,
        string? title,
        string? privateNote,
        DateTime nowUtc)
    {
        return new Visit(
            id,
            userId,
            parkId,
            date,
            timeZoneId,
            serviceDayConvention,
            VisitStatus.Draft,
            VisitPrivacy.Private,
            title,
            privateNote,
            1,
            nowUtc,
            nowUtc,
            null);
    }

    public static Visit Restore(
        VisitId id,
        string userId,
        string parkId,
        VisitDate date,
        string? timeZoneId,
        LocalServiceDayConvention serviceDayConvention,
        VisitStatus status,
        VisitPrivacy privacy,
        string? title,
        string? privateNote,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? completedAtUtc)
    {
        return new Visit(
            id,
            userId,
            parkId,
            date,
            timeZoneId,
            serviceDayConvention,
            status,
            privacy,
            title,
            privateNote,
            version,
            createdAtUtc,
            updatedAtUtc,
            completedAtUtc);
    }

    public void UpdateDraft(
        VisitDate date,
        string? timeZoneId,
        LocalServiceDayConvention serviceDayConvention,
        string? title,
        string? privateNote,
        DateTime nowUtc)
    {
        if (this.Status != VisitStatus.Draft)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidTransition,
                "Only a draft visit can be edited.");
        }

        ArgumentNullException.ThrowIfNull(date);
        ValidateServiceDayConvention(serviceDayConvention);
        string? normalizedTimeZoneId = NormalizeTimeZoneId(timeZoneId);
        string? normalizedTitle = NormalizeTitle(title);
        string? normalizedPrivateNote = NormalizePrivateNote(privateNote);
        this.ValidateMutationTimestamp(nowUtc);

        if (this.Date == date
            && string.Equals(this.TimeZoneId, normalizedTimeZoneId, StringComparison.Ordinal)
            && this.ServiceDayConvention == serviceDayConvention
            && string.Equals(this.Title, normalizedTitle, StringComparison.Ordinal)
            && string.Equals(this.PrivateNote, normalizedPrivateNote, StringComparison.Ordinal))
        {
            return;
        }

        this.PrepareMutation(nowUtc);
        this.Date = date;
        this.TimeZoneId = normalizedTimeZoneId;
        this.ServiceDayConvention = serviceDayConvention;
        this.Title = normalizedTitle;
        this.PrivateNote = normalizedPrivateNote;
        this.CommitMutation(nowUtc);
    }

    public void Complete(DateOnly parkLocalToday, DateTime nowUtc)
    {
        if (this.Status != VisitStatus.Draft)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidTransition,
                "Only a draft visit can be completed.");
        }

        this.EnsureNotEntirelyInFuture(parkLocalToday);
        this.PrepareMutation(nowUtc);
        this.Status = VisitStatus.Completed;
        this.CompletedAtUtc = nowUtc;
        this.CommitMutation(nowUtc);
    }

    public void Reopen(DateTime nowUtc)
    {
        if (this.Status != VisitStatus.Completed)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidTransition,
                "Only a completed visit can be reopened.");
        }

        this.PrepareMutation(nowUtc);
        this.Status = VisitStatus.Draft;
        this.CompletedAtUtc = null;
        this.CommitMutation(nowUtc);
    }

    public void Archive(DateTime nowUtc)
    {
        if (this.Status == VisitStatus.Archived)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidTransition,
                "An archived visit cannot be archived again.");
        }

        this.PrepareMutation(nowUtc);
        this.Status = VisitStatus.Archived;
        this.CommitMutation(nowUtc);
    }

    public void RestoreAsDraft(DateTime nowUtc)
    {
        if (this.Status != VisitStatus.Archived)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidTransition,
                "Only an archived visit can be restored.");
        }

        this.PrepareMutation(nowUtc);
        this.Status = VisitStatus.Draft;
        this.CompletedAtUtc = null;
        this.CommitMutation(nowUtc);
    }

    public void RestoreAsCompleted(DateOnly parkLocalToday, DateTime nowUtc)
    {
        if (this.Status != VisitStatus.Archived)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidTransition,
                "Only an archived visit can be restored.");
        }

        this.EnsureNotEntirelyInFuture(parkLocalToday);
        this.PrepareMutation(nowUtc);
        this.Status = VisitStatus.Completed;
        this.CompletedAtUtc ??= nowUtc;
        this.CommitMutation(nowUtc);
    }

    private static void ValidateServiceDayConvention(LocalServiceDayConvention serviceDayConvention)
    {
        if (!Enum.IsDefined(serviceDayConvention))
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidServiceDayConvention,
                "The service day convention is invalid.");
        }
    }

    private static void ValidateStatus(VisitStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidStatus,
                "The visit status is invalid.");
        }
    }

    private static void ValidatePrivacy(VisitPrivacy privacy)
    {
        if (privacy != VisitPrivacy.Private)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidPrivacy,
                "Only private visits are supported in this version.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 1)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidVersion,
                "The visit version must be positive.");
        }
    }

    private static void ValidateRestoredTimestamps(
        VisitStatus status,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? completedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (completedAtUtc.HasValue)
        {
            EnsureUtc(completedAtUtc.Value);
        }

        if (updatedAtUtc < createdAtUtc
            || (completedAtUtc.HasValue
                && (completedAtUtc.Value < createdAtUtc || completedAtUtc.Value > updatedAtUtc)))
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidTimestampOrder,
                "The visit timestamps are not chronologically consistent.");
        }

        if (status == VisitStatus.Completed && !completedAtUtc.HasValue)
        {
            throw CreateValidationException(
                VisitErrorCodes.CompletedAtRequired,
                "A completed visit requires a completion timestamp.");
        }

        if (status == VisitStatus.Draft && completedAtUtc.HasValue)
        {
            throw CreateValidationException(
                VisitErrorCodes.CompletedAtForbidden,
                "A draft visit cannot have a completion timestamp.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw CreateValidationException(
                VisitErrorCodes.TimestampNotUtc,
                "Visit timestamps must be expressed in UTC.");
        }
    }

    private static string? NormalizeTitle(string? value)
    {
        string? normalizedValue = NormalizeOptional(value);
        if (normalizedValue is null)
        {
            return null;
        }

        if (normalizedValue.Length > MaximumTitleLength)
        {
            throw CreateValidationException(
                VisitErrorCodes.TitleTooLong,
                $"The visit title cannot exceed {MaximumTitleLength} characters.");
        }

        foreach (char character in normalizedValue)
        {
            UnicodeCategory category = char.GetUnicodeCategory(character);
            if (char.IsControl(character)
                || category is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
            {
                throw CreateValidationException(
                    VisitErrorCodes.TitleControlCharacter,
                    "The visit title cannot contain control characters.");
            }
        }

        return normalizedValue;
    }

    private static string? NormalizePrivateNote(string? value)
    {
        string? normalizedValue = NormalizeOptional(value);
        if (normalizedValue is not null && normalizedValue.Length > MaximumPrivateNoteLength)
        {
            throw CreateValidationException(
                VisitErrorCodes.PrivateNoteTooLong,
                $"The private visit note cannot exceed {MaximumPrivateNoteLength} characters.");
        }

        return normalizedValue;
    }

    private static string? NormalizeTimeZoneId(string? value)
    {
        string? normalizedValue = NormalizeOptional(value);
        if (normalizedValue is null)
        {
            return null;
        }

        if (normalizedValue.Length > MaximumTimeZoneIdLength)
        {
            throw CreateValidationException(
                VisitErrorCodes.TimeZoneIdTooLong,
                $"The visit time zone identifier cannot exceed {MaximumTimeZoneIdLength} characters.");
        }

        foreach (char character in normalizedValue)
        {
            if (char.IsControl(character))
            {
                throw CreateValidationException(
                    VisitErrorCodes.TimeZoneIdControlCharacter,
                    "The visit time zone identifier cannot contain control characters.");
            }
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        return normalizedValue.Length == 0 ? null : normalizedValue;
    }

    private static VisitValidationException CreateValidationException(string errorCode, string message)
    {
        return new VisitValidationException(errorCode, message);
    }

    private void EnsureNotEntirelyInFuture(DateOnly parkLocalToday)
    {
        if (this.Date.GetEarliestPossibleDate() > parkLocalToday)
        {
            throw CreateValidationException(
                VisitErrorCodes.FutureCompletedDate,
                "A completed visit cannot be entirely in the future.");
        }
    }

    private void PrepareMutation(DateTime nowUtc)
    {
        this.ValidateMutationTimestamp(nowUtc);
        if (this.Version == long.MaxValue)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidVersion,
                "The visit version cannot be incremented further.");
        }
    }

    private void ValidateMutationTimestamp(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw CreateValidationException(
                VisitErrorCodes.InvalidTimestampOrder,
                "A visit mutation cannot predate the current state.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }
}
