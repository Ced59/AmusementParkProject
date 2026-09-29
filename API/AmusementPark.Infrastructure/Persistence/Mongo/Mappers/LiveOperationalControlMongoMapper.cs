using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

public static class LiveOperationalControlMongoMapper
{
    public static LiveOperationalControlDocument ToDocument(this LiveOperationalControl control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return new LiveOperationalControlDocument
        {
            Id = $"{control.Id:N}:{control.Revision}",
            ControlId = control.Id.ToString("N"),
            Version = control.Version,
            ScopeType = control.Scope.Type,
            SourceId = control.Scope.SourceId.Value,
            ExternalEntityId = control.Scope.ExternalEntityId,
            InternalParkId = control.Scope.InternalParkId,
            TargetType = control.Scope.TargetType,
            InternalTargetId = control.Scope.InternalTargetId,
            CollectionEnabled = control.CollectionEnabled,
            PublicReadEnabled = control.PublicReadEnabled,
            Revision = control.Revision,
            SupersedesRevision = control.SupersedesRevision,
            ChangedByUserId = control.ChangedByUserId,
            Reason = control.Reason,
            RecordedAtUtc = control.RecordedAtUtc,
            CreatedAt = control.RecordedAtUtc,
            UpdatedAt = control.RecordedAtUtc,
        };
    }

    public static LiveOperationalControl ToDomain(this LiveOperationalControlDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new LiveOperationalControl(
            Guid.ParseExact(document.ControlId, "N"),
            new LiveOperationalControlScope(
                document.ScopeType,
                LiveDataSourceId.Parse(document.SourceId),
                document.ExternalEntityId,
                document.InternalParkId,
                document.TargetType,
                document.InternalTargetId),
            document.CollectionEnabled,
            document.PublicReadEnabled,
            document.Revision,
            document.SupersedesRevision,
            document.ChangedByUserId,
            document.Reason,
            DateTime.SpecifyKind(document.RecordedAtUtc, DateTimeKind.Utc),
            document.Version);
    }
}
