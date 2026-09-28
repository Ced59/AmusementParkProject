using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public static class LiveTargetMappingResultFactory
{
    public static LiveTargetMappingResult Create(ExternalLiveTargetMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ExternalLiveTargetDescriptor externalTarget = mapping.ExternalTarget;
        LiveTargetReference? target = mapping.Target;
        return new LiveTargetMappingResult(
            mapping.Id,
            mapping.Version,
            mapping.SourceId.Value,
            new LiveExternalTargetResult(
                externalTarget.Type,
                externalTarget.Id,
                externalTarget.ParentId,
                externalTarget.DisplayName,
                externalTarget.ParentDisplayName,
                externalTarget.CountryCode),
            target is null
                ? null
                : new LiveInternalTargetResult(
                    target.Type,
                    target.Id,
                    target.ParkId,
                    target.DisplayName,
                    target.ParkDisplayName,
                    target.CountryCode),
            mapping.Status,
            mapping.Confidence,
            mapping.ValidFromUtc,
            mapping.ValidToUtc,
            mapping.Revision,
            mapping.SupersedesRevision,
            mapping.ReviewedByUserId,
            mapping.ReviewNote,
            mapping.RecordedAtUtc,
            mapping.IsEligibleForLiveUse);
    }
}
