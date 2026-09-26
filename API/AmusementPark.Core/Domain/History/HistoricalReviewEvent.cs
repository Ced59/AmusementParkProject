namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalReviewEvent
{
    public HistoricalReviewEvent(
        Guid id,
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int resourceRevision,
        HistoricalReviewEventType eventType,
        string actorUserId,
        string? privateNote,
        DateTime occurredAtUtc)
    {
        if (id == Guid.Empty || resourceId == Guid.Empty)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidIdentifier, "A historical review event requires identifiers.");
        }

        if (!Enum.IsDefined(resourceType) || !Enum.IsDefined(eventType) || resourceRevision < 1)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidReviewEvent, "The historical review event target is invalid.");
        }

        string normalizedActorUserId = actorUserId?.Trim() ?? string.Empty;
        if (normalizedActorUserId.Length == 0
            || normalizedActorUserId.Length > 200
            || normalizedActorUserId.Any(char.IsControl))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidReviewEvent, "A historical review event requires a valid actor.");
        }

        string? normalizedPrivateNote = string.IsNullOrWhiteSpace(privateNote) ? null : privateNote.Trim();
        if (normalizedPrivateNote?.Length > 4000)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidText, "A historical review note is too long.");
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidTimestamp, "Historical review timestamps must be UTC.");
        }

        this.Id = id;
        this.ResourceType = resourceType;
        this.ResourceId = resourceId;
        this.ResourceRevision = resourceRevision;
        this.EventType = eventType;
        this.ActorUserId = normalizedActorUserId;
        this.PrivateNote = normalizedPrivateNote;
        this.OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; }

    public HistoricalReviewResourceType ResourceType { get; }

    public Guid ResourceId { get; }

    public int ResourceRevision { get; }

    public HistoricalReviewEventType EventType { get; }

    public string ActorUserId { get; }

    public string? PrivateNote { get; }

    public DateTime OccurredAtUtc { get; }

    private static HistoricalPersistenceValidationException Invalid(string code, string message)
    {
        return new HistoricalPersistenceValidationException(code, message);
    }
}
