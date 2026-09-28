using System.Globalization;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

public static class LiveTargetMappingMongoMapper
{
    public static ExternalLiveTargetMappingDocument ToDocument(
        this ExternalLiveTargetMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ExternalLiveTargetDescriptor externalTarget = mapping.ExternalTarget;
        LiveTargetReference? target = mapping.Target;
        return new ExternalLiveTargetMappingDocument
        {
            Id = $"{mapping.Id:N}:{mapping.Revision.ToString(CultureInfo.InvariantCulture)}",
            CreatedAt = mapping.RecordedAtUtc,
            UpdatedAt = mapping.RecordedAtUtc,
            MappingId = mapping.Id.ToString("N", CultureInfo.InvariantCulture),
            SourceId = mapping.SourceId.Value,
            ExternalTarget = new ExternalLiveTargetDescriptorDocument
            {
                Type = externalTarget.Type,
                Id = externalTarget.Id,
                ParentId = externalTarget.ParentId,
                DisplayName = externalTarget.DisplayName,
                ParentDisplayName = externalTarget.ParentDisplayName,
                CountryCode = externalTarget.CountryCode,
            },
            Target = target is null
                ? null
                : new LiveTargetReferenceDocument
                {
                    Type = target.Type,
                    Id = target.Id,
                    ParkId = target.ParkId,
                    DisplayName = target.DisplayName,
                    ParkDisplayName = target.ParkDisplayName,
                    CountryCode = target.CountryCode,
                },
            Status = mapping.Status,
            Confidence = mapping.Confidence,
            ValidFromUtc = mapping.ValidFromUtc,
            ValidToUtc = mapping.ValidToUtc,
            Revision = mapping.Revision,
            SupersedesRevision = mapping.SupersedesRevision,
            ReviewedByUserId = mapping.ReviewedByUserId,
            ReviewNote = mapping.ReviewNote,
            RecordedAtUtc = mapping.RecordedAtUtc,
        };
    }

    public static ExternalLiveTargetMapping ToDomain(
        this ExternalLiveTargetMappingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ExternalLiveTargetDescriptorDocument externalTarget = document.ExternalTarget;
        LiveTargetReferenceDocument? target = document.Target;
        return new ExternalLiveTargetMapping(
            Guid.Parse(document.MappingId),
            LiveDataSourceId.Parse(document.SourceId),
            new ExternalLiveTargetDescriptor(
                externalTarget.Type,
                externalTarget.Id,
                externalTarget.ParentId,
                externalTarget.DisplayName,
                externalTarget.ParentDisplayName,
                externalTarget.CountryCode),
            target is null
                ? null
                : new LiveTargetReference(
                    target.Type,
                    target.Id,
                    target.ParkId,
                    target.DisplayName,
                    target.ParkDisplayName,
                    target.CountryCode),
            document.Status,
            document.Confidence,
            DateTime.SpecifyKind(document.ValidFromUtc, DateTimeKind.Utc),
            document.ValidToUtc.HasValue
                ? DateTime.SpecifyKind(document.ValidToUtc.Value, DateTimeKind.Utc)
                : null,
            document.Revision,
            document.SupersedesRevision,
            document.ReviewedByUserId,
            document.ReviewNote,
            DateTime.SpecifyKind(document.RecordedAtUtc, DateTimeKind.Utc));
    }
}
