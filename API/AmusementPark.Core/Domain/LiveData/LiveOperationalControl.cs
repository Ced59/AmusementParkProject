using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveOperationalControl
{
    public const string CurrentVersion = "live-operational-control-v1";

    public LiveOperationalControl(
        Guid id,
        LiveOperationalControlScope scope,
        bool collectionEnabled,
        bool publicReadEnabled,
        int revision,
        int? supersedesRevision,
        string changedByUserId,
        string reason,
        DateTime recordedAtUtc,
        string version = CurrentVersion)
    {
        if (id == Guid.Empty
            || revision < 1
            || (supersedesRevision != revision - 1 && supersedesRevision is not null))
        {
            throw Invalid("The live operational control revision is invalid.");
        }

        ArgumentNullException.ThrowIfNull(scope);
        if (recordedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw Invalid("The live operational control timestamp must be UTC.");
        }

        string normalizedUserId = IdentifierRules.NormalizeRequired(
            changedByUserId,
            nameof(changedByUserId));
        string normalizedReason = NormalizeReason(reason);
        if (!string.Equals(version, CurrentVersion, StringComparison.Ordinal))
        {
            throw Invalid("The live operational control version is unsupported.");
        }

        this.Id = id;
        this.Scope = scope;
        this.CollectionEnabled = collectionEnabled;
        this.PublicReadEnabled = publicReadEnabled;
        this.Revision = revision;
        this.SupersedesRevision = supersedesRevision;
        this.ChangedByUserId = normalizedUserId;
        this.Reason = normalizedReason;
        this.RecordedAtUtc = recordedAtUtc;
        this.Version = version;
    }

    public Guid Id { get; }

    public LiveOperationalControlScope Scope { get; }

    public bool CollectionEnabled { get; }

    public bool PublicReadEnabled { get; }

    public int Revision { get; }

    public int? SupersedesRevision { get; }

    public string ChangedByUserId { get; }

    public string Reason { get; }

    public DateTime RecordedAtUtc { get; }

    public string Version { get; }

    public LiveOperationalControl Revise(
        bool collectionEnabled,
        bool publicReadEnabled,
        string changedByUserId,
        string reason,
        DateTime recordedAtUtc)
    {
        if (recordedAtUtc < this.RecordedAtUtc)
        {
            throw Invalid("A live operational control revision cannot predate its predecessor.");
        }

        return new LiveOperationalControl(
            this.Id,
            this.Scope,
            collectionEnabled,
            publicReadEnabled,
            checked(this.Revision + 1),
            this.Revision,
            changedByUserId,
            reason,
            recordedAtUtc,
            this.Version);
    }

    private static string NormalizeReason(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 3 or > 500 || normalized.Any(char.IsControl))
        {
            throw Invalid("A reason between 3 and 500 characters is required.");
        }

        return normalized;
    }

    private static LiveDataValidationException Invalid(string message)
    {
        return new LiveDataValidationException(
            LiveDataErrorCodes.InvalidOperationalControl,
            message);
    }
}
