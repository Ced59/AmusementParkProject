using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

/// <summary>
/// Intention personnelle privée envers un parc ou un élément de parc.
/// </summary>
public sealed class UserCollectionEntry
{
    public const int MinimumPriority = 1;
    public const int MaximumPriority = 5;
    public const int MaximumPrivateNoteLength = 2000;
    public const int MaximumEntriesPerUser = 500;

    private UserCollectionEntry(
        UserCollectionEntryId id,
        string userId,
        CollectionTargetType targetType,
        string targetId,
        UserCollectionKind kind,
        CollectionTargetStatus targetStatus,
        string? privateNote,
        int? priority,
        DateRangePreference? preferredPeriod,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        _ = id.Value;
        ValidateTargetType(targetType);
        ValidateKind(kind);
        ValidateCompatibility(targetType, kind);
        ValidateTargetStatus(targetStatus);
        ValidatePriority(priority);
        ValidateTimestamps(createdAtUtc, updatedAtUtc);
        if (version < 1)
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidVersion,
                "The collection entry version must be positive.");
        }

        this.Id = id;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.TargetType = targetType;
        this.TargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        this.Kind = kind;
        this.TargetStatus = targetStatus;
        this.PrivateNote = NormalizePrivateNote(privateNote);
        this.Priority = priority;
        this.PreferredPeriod = preferredPeriod;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public UserCollectionEntryId Id { get; }

    public string UserId { get; }

    public CollectionTargetType TargetType { get; }

    public string TargetId { get; }

    public UserCollectionKind Kind { get; }

    public CollectionTargetStatus TargetStatus { get; private set; }

    public string? PrivateNote { get; private set; }

    public int? Priority { get; private set; }

    public DateRangePreference? PreferredPeriod { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static UserCollectionEntry Create(
        UserCollectionEntryId id,
        string userId,
        CollectionTargetType targetType,
        string targetId,
        UserCollectionKind kind,
        CollectionTargetStatus targetStatus,
        string? privateNote,
        int? priority,
        DateRangePreference? preferredPeriod,
        DateTime nowUtc)
    {
        return new UserCollectionEntry(
            id,
            userId,
            targetType,
            targetId,
            kind,
            targetStatus,
            privateNote,
            priority,
            preferredPeriod,
            nowUtc,
            nowUtc,
            1);
    }

    public static UserCollectionEntry Restore(
        UserCollectionEntryId id,
        string userId,
        CollectionTargetType targetType,
        string targetId,
        UserCollectionKind kind,
        CollectionTargetStatus targetStatus,
        string? privateNote,
        int? priority,
        DateRangePreference? preferredPeriod,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        return new UserCollectionEntry(
            id,
            userId,
            targetType,
            targetId,
            kind,
            targetStatus,
            privateNote,
            priority,
            preferredPeriod,
            createdAtUtc,
            updatedAtUtc,
            version);
    }

    public void UpdatePreferences(
        string? privateNote,
        int? priority,
        DateRangePreference? preferredPeriod,
        DateTime nowUtc)
    {
        string? normalizedPrivateNote = NormalizePrivateNote(privateNote);
        ValidatePriority(priority);
        this.ValidateMutationTimestamp(nowUtc);

        if (string.Equals(this.PrivateNote, normalizedPrivateNote, StringComparison.Ordinal)
            && this.Priority == priority
            && this.PreferredPeriod == preferredPeriod)
        {
            return;
        }

        this.PrepareMutation();
        this.PrivateNote = normalizedPrivateNote;
        this.Priority = priority;
        this.PreferredPeriod = preferredPeriod;
        this.CommitMutation(nowUtc);
    }

    /// <summary>
    /// Met à jour le statut factuel sans supprimer l'intention de l'utilisateur.
    /// </summary>
    public void SynchronizeTargetStatus(
        CollectionTargetStatus targetStatus,
        DateTime nowUtc)
    {
        ValidateTargetStatus(targetStatus);
        this.ValidateMutationTimestamp(nowUtc);
        if (this.TargetStatus == targetStatus)
        {
            return;
        }

        this.PrepareMutation();
        this.TargetStatus = targetStatus;
        this.CommitMutation(nowUtc);
    }

    public bool HasSameLogicalIdentityAs(UserCollectionEntry other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return string.Equals(this.UserId, other.UserId, StringComparison.Ordinal)
            && this.TargetType == other.TargetType
            && string.Equals(this.TargetId, other.TargetId, StringComparison.Ordinal)
            && this.Kind == other.Kind;
    }

    private static void ValidateTargetType(CollectionTargetType targetType)
    {
        if (!Enum.IsDefined(targetType))
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidTargetType,
                "The collection target type is invalid.");
        }
    }

    private static void ValidateKind(UserCollectionKind kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidKind,
                "The collection kind is invalid.");
        }
    }

    private static void ValidateCompatibility(
        CollectionTargetType targetType,
        UserCollectionKind kind)
    {
        bool isCompatible = kind switch
        {
            UserCollectionKind.Favorite => true,
            UserCollectionKind.WantToVisit => targetType == CollectionTargetType.Park,
            UserCollectionKind.WantToExperience => targetType == CollectionTargetType.ParkItem,
            UserCollectionKind.Planned => true,
            _ => false,
        };

        if (!isCompatible)
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.IncompatibleTarget,
                "The collection intention is incompatible with the selected target type.");
        }
    }

    private static void ValidateTargetStatus(CollectionTargetStatus targetStatus)
    {
        if (!Enum.IsDefined(targetStatus))
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidTargetStatus,
                "The collection target status is invalid.");
        }
    }

    private static void ValidatePriority(int? priority)
    {
        if (priority is < MinimumPriority or > MaximumPriority)
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidPriority,
                $"The collection priority must be between {MinimumPriority} and {MaximumPriority}.");
        }
    }

    private static string? NormalizePrivateNote(string? privateNote)
    {
        string normalizedPrivateNote = privateNote?.Trim() ?? string.Empty;
        if (normalizedPrivateNote.Length == 0)
        {
            return null;
        }

        if (normalizedPrivateNote.Length > MaximumPrivateNoteLength)
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.PrivateNoteTooLong,
                $"The private collection note cannot exceed {MaximumPrivateNoteLength} characters.");
        }

        return normalizedPrivateNote;
    }

    private static void ValidateTimestamps(DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidTimestamp,
                "The collection entry timestamps are not chronologically consistent.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidTimestamp,
                "Collection entry timestamps must be expressed in UTC.");
        }
    }

    private void ValidateMutationTimestamp(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidTimestamp,
                "A collection entry mutation cannot predate its current state.");
        }
    }

    private void PrepareMutation()
    {
        if (this.Version == long.MaxValue)
        {
            throw CreateValidationException(
                UserCollectionErrorCodes.InvalidVersion,
                "The collection entry version cannot be incremented further.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static UserCollectionValidationException CreateValidationException(
        string code,
        string message)
    {
        return new UserCollectionValidationException(code, message);
    }
}
