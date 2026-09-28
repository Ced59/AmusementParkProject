using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Contracts.LiveData;

namespace AmusementPark.WebAPI.Mappers;

public static class LiveDataHttpMapper
{
    public static CreateLiveTargetMappingCandidateCommand ToCommand(
        this CreateLiveTargetMappingCandidateRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CreateLiveTargetMappingCandidateCommand(
            request.SourceId,
            (LiveTargetType)request.TargetType,
            request.ExternalTargetId,
            request.ExternalParentTargetId,
            request.ExternalDisplayName,
            request.ExternalParentDisplayName,
            request.ExternalCountryCode,
            request.SuggestedInternalTargetId,
            request.SuggestedParkId);
    }

    public static ReviewLiveTargetMappingCommand ToCommand(
        this ReviewLiveTargetMappingRequestDto request,
        Guid mappingId,
        string reviewerUserId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ReviewLiveTargetMappingCommand(
            mappingId,
            request.ExpectedRevision,
            (LiveTargetMappingDecision)request.Decision,
            request.InternalTargetId,
            request.ParkId,
            reviewerUserId,
            request.ReviewNote);
    }

    public static LiveTargetMappingDto ToHttp(this LiveTargetMappingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new LiveTargetMappingDto
        {
            MappingId = result.MappingId,
            Version = result.Version,
            SourceId = result.SourceId,
            ExternalTarget = result.ExternalTarget.ToHttp(),
            Target = result.Target?.ToHttp(),
            Status = (LiveMappingStatusDto)result.Status,
            Confidence = (LiveMappingConfidenceDto)result.Confidence,
            ValidFromUtc = result.ValidFromUtc,
            ValidToUtc = result.ValidToUtc,
            Revision = result.Revision,
            SupersedesRevision = result.SupersedesRevision,
            ReviewedByUserId = result.ReviewedByUserId,
            ReviewNote = result.ReviewNote,
            RecordedAtUtc = result.RecordedAtUtc,
            IsEligibleForLiveUse = result.IsEligibleForLiveUse,
        };
    }

    private static LiveExternalTargetDto ToHttp(this LiveExternalTargetResult result)
    {
        return new LiveExternalTargetDto
        {
            Type = (LiveTargetTypeDto)result.Type,
            Id = result.Id,
            ParentId = result.ParentId,
            DisplayName = result.DisplayName,
            ParentDisplayName = result.ParentDisplayName,
            CountryCode = result.CountryCode,
        };
    }

    private static LiveInternalTargetDto ToHttp(this LiveInternalTargetResult result)
    {
        return new LiveInternalTargetDto
        {
            Type = (LiveTargetTypeDto)result.Type,
            Id = result.Id,
            ParkId = result.ParkId,
            DisplayName = result.DisplayName,
            ParkDisplayName = result.ParkDisplayName,
            CountryCode = result.CountryCode,
        };
    }
}
