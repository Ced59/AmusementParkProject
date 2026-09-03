using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Nature d'une preuve privée et immuable produite par une mutation du passeport.
/// </summary>
public enum PassportAuditEventType
{
    VisitCreated = 1,
    VisitDateChanged = 2,
    VisitCompleted = 3,
    VisitReopened = 4,
    VisitArchived = 5,
    VisitDeleted = 6,
    ParkAssessmentCreated = 7,
    ParkAssessmentChanged = 8,
    ParkAssessmentDeleted = 9,
    RideOccurrenceAdded = 10,
    RideOccurrenceChanged = 11,
    RideOccurrenceDeleted = 12,
    RideAssessmentCreated = 13,
    RideAssessmentChanged = 14,
    RideAssessmentDeleted = 15,
    VisitMetadataChanged = 16,
}

public enum PassportAuditEntityType
{
    Visit = 1,
    ParkAssessment = 2,
    RideOccurrence = 3,
    RideAssessment = 4,
}

public enum PassportAuditOrigin
{
    User = 1,
    Import = 2,
    System = 3,
}

public enum PassportAuditChangedField
{
    Visit = 1,
    Date = 2,
    Status = 3,
    ParkAssessmentRating = 4,
    ParkAssessmentPrivateComment = 5,
    RideOccurrence = 6,
    Moment = 7,
    HistoricalConsistency = 8,
    HistoricalTarget = 9,
    PrivateNote = 10,
    SortPosition = 11,
    DeletedAtUtc = 12,
    RideAssessmentRating = 13,
    RideAssessmentPrivateComment = 14,
    AssessmentRevision = 15,
    TimeZone = 16,
    ServiceDayConvention = 17,
    Title = 18,
}

/// <summary>
/// Preuve d'audit minimisée. Les textes privés et les heures locales ne sont jamais copiés.
/// </summary>
public sealed class PassportAuditEvent
{
    private PassportAuditEvent(
        string id,
        string userId,
        PassportAuditEntityType entityType,
        string entityId,
        string visitId,
        string parkId,
        string? parkItemId,
        PassportAuditEventType eventType,
        long entityVersion,
        int? assessmentRevision,
        IReadOnlyCollection<PassportAuditChangedField> changedFields,
        byte? previousRatingHalfSteps,
        byte? newRatingHalfSteps,
        VisitDate? previousVisitDate,
        VisitDate? newVisitDate,
        VisitStatus? previousVisitStatus,
        VisitStatus? newVisitStatus,
        RideOccurrenceStatus? previousRideStatus,
        RideOccurrenceStatus? newRideStatus,
        long? previousSortPosition,
        long? newSortPosition,
        bool privateTextChanged,
        string correlationId,
        PassportAuditOrigin origin,
        DateTime occurredAtUtc)
    {
        ValidateEnum(entityType, nameof(entityType));
        ValidateEnum(eventType, nameof(eventType));
        ValidateEnum(origin, nameof(origin));
        ValidateOptionalEnum(previousVisitStatus, nameof(previousVisitStatus));
        ValidateOptionalEnum(newVisitStatus, nameof(newVisitStatus));
        ValidateOptionalEnum(previousRideStatus, nameof(previousRideStatus));
        ValidateOptionalEnum(newRideStatus, nameof(newRideStatus));
        if (entityVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(entityVersion));
        }

        if (assessmentRevision is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(assessmentRevision));
        }

        ValidateHalfSteps(previousRatingHalfSteps, nameof(previousRatingHalfSteps));
        ValidateHalfSteps(newRatingHalfSteps, nameof(newRatingHalfSteps));
        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The audit timestamp must be UTC.", nameof(occurredAtUtc));
        }

        this.Id = IdentifierRules.NormalizeRequired(id, nameof(id));
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.EntityType = entityType;
        this.EntityId = IdentifierRules.NormalizeRequired(entityId, nameof(entityId));
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.ParkItemId = NormalizeOptionalIdentifier(parkItemId, nameof(parkItemId));
        this.EventType = eventType;
        this.EntityVersion = entityVersion;
        this.AssessmentRevision = assessmentRevision;
        this.ChangedFields = NormalizeChangedFields(changedFields);
        this.PreviousRatingHalfSteps = previousRatingHalfSteps;
        this.NewRatingHalfSteps = newRatingHalfSteps;
        this.PreviousVisitDate = previousVisitDate;
        this.NewVisitDate = newVisitDate;
        this.PreviousVisitStatus = previousVisitStatus;
        this.NewVisitStatus = newVisitStatus;
        this.PreviousRideStatus = previousRideStatus;
        this.NewRideStatus = newRideStatus;
        this.PreviousSortPosition = previousSortPosition;
        this.NewSortPosition = newSortPosition;
        this.PrivateTextChanged = privateTextChanged;
        this.CorrelationId = IdentifierRules.NormalizeRequired(
            correlationId,
            nameof(correlationId));
        this.Origin = origin;
        this.OccurredAtUtc = occurredAtUtc;
    }

    public string Id { get; }

    public string UserId { get; }

    public PassportAuditEntityType EntityType { get; }

    public string EntityId { get; }

    public string VisitId { get; }

    public string ParkId { get; }

    public string? ParkItemId { get; }

    public PassportAuditEventType EventType { get; }

    public long EntityVersion { get; }

    public int? AssessmentRevision { get; }

    public IReadOnlyCollection<PassportAuditChangedField> ChangedFields { get; }

    public byte? PreviousRatingHalfSteps { get; }

    public byte? NewRatingHalfSteps { get; }

    public VisitDate? PreviousVisitDate { get; }

    public VisitDate? NewVisitDate { get; }

    public VisitStatus? PreviousVisitStatus { get; }

    public VisitStatus? NewVisitStatus { get; }

    public RideOccurrenceStatus? PreviousRideStatus { get; }

    public RideOccurrenceStatus? NewRideStatus { get; }

    public long? PreviousSortPosition { get; }

    public long? NewSortPosition { get; }

    public bool PrivateTextChanged { get; }

    public string CorrelationId { get; }

    public PassportAuditOrigin Origin { get; }

    public DateTime OccurredAtUtc { get; }

    public static PassportAuditEvent Create(
        string userId,
        PassportAuditEntityType entityType,
        string entityId,
        string visitId,
        string parkId,
        string? parkItemId,
        PassportAuditEventType eventType,
        long entityVersion,
        int? assessmentRevision,
        IReadOnlyCollection<PassportAuditChangedField> changedFields,
        byte? previousRatingHalfSteps,
        byte? newRatingHalfSteps,
        VisitDate? previousVisitDate,
        VisitDate? newVisitDate,
        VisitStatus? previousVisitStatus,
        VisitStatus? newVisitStatus,
        RideOccurrenceStatus? previousRideStatus,
        RideOccurrenceStatus? newRideStatus,
        long? previousSortPosition,
        long? newSortPosition,
        bool privateTextChanged,
        string correlationSeed,
        PassportAuditOrigin origin,
        DateTime occurredAtUtc)
    {
        string normalizedEntityId = IdentifierRules.NormalizeRequired(entityId, nameof(entityId));
        string id = $"{entityType}:{normalizedEntityId}:{entityVersion}:{eventType}";
        return new PassportAuditEvent(
            id,
            userId,
            entityType,
            normalizedEntityId,
            visitId,
            parkId,
            parkItemId,
            eventType,
            entityVersion,
            assessmentRevision,
            changedFields,
            previousRatingHalfSteps,
            newRatingHalfSteps,
            previousVisitDate,
            newVisitDate,
            previousVisitStatus,
            newVisitStatus,
            previousRideStatus,
            newRideStatus,
            previousSortPosition,
            newSortPosition,
            privateTextChanged,
            HashCorrelation(correlationSeed),
            origin,
            occurredAtUtc);
    }

    public static PassportAuditEvent Restore(
        string id,
        string userId,
        PassportAuditEntityType entityType,
        string entityId,
        string visitId,
        string parkId,
        string? parkItemId,
        PassportAuditEventType eventType,
        long entityVersion,
        int? assessmentRevision,
        IReadOnlyCollection<PassportAuditChangedField> changedFields,
        byte? previousRatingHalfSteps,
        byte? newRatingHalfSteps,
        VisitDate? previousVisitDate,
        VisitDate? newVisitDate,
        VisitStatus? previousVisitStatus,
        VisitStatus? newVisitStatus,
        RideOccurrenceStatus? previousRideStatus,
        RideOccurrenceStatus? newRideStatus,
        long? previousSortPosition,
        long? newSortPosition,
        bool privateTextChanged,
        string correlationId,
        PassportAuditOrigin origin,
        DateTime occurredAtUtc)
    {
        return new PassportAuditEvent(
            id,
            userId,
            entityType,
            entityId,
            visitId,
            parkId,
            parkItemId,
            eventType,
            entityVersion,
            assessmentRevision,
            changedFields,
            previousRatingHalfSteps,
            newRatingHalfSteps,
            previousVisitDate,
            newVisitDate,
            previousVisitStatus,
            newVisitStatus,
            previousRideStatus,
            newRideStatus,
            previousSortPosition,
            newSortPosition,
            privateTextChanged,
            correlationId,
            origin,
            occurredAtUtc);
    }

    private static string HashCorrelation(string value)
    {
        string normalizedValue = IdentifierRules.NormalizeRequired(value, nameof(value));
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedValue));
        return Convert.ToHexStringLower(bytes);
    }

    private static IReadOnlyCollection<PassportAuditChangedField> NormalizeChangedFields(
        IReadOnlyCollection<PassportAuditChangedField> changedFields)
    {
        ArgumentNullException.ThrowIfNull(changedFields);
        PassportAuditChangedField[] normalized = changedFields
            .Distinct()
            .OrderBy(static field => field)
            .ToArray();
        if (normalized.Length == 0 || normalized.Any(static field => !Enum.IsDefined(field)))
        {
            throw new ArgumentException(
                "At least one valid changed field is required.",
                nameof(changedFields));
        }

        return normalized;
    }

    private static string? NormalizeOptionalIdentifier(string? value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : IdentifierRules.NormalizeRequired(value, parameterName);
    }

    private static void ValidateHalfSteps(byte? halfSteps, string parameterName)
    {
        if (halfSteps.HasValue && halfSteps.Value is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateOptionalEnum<TEnum>(TEnum? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (value.HasValue && !Enum.IsDefined(value.Value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
