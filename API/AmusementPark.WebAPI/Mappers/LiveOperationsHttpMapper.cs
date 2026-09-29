using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Contracts.LiveData;

namespace AmusementPark.WebAPI.Mappers;

public static class LiveOperationsHttpMapper
{
    public static UpdateLiveOperationalControlCommand ToCommand(
        this UpdateLiveOperationalControlRequestDto request,
        string changedByUserId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateLiveOperationalControlCommand(
            (LiveOperationalScopeType)request.ScopeType,
            request.SourceId,
            request.ExternalEntityId,
            request.InternalParkId,
            request.TargetType.HasValue ? (LiveTargetType)request.TargetType.Value : null,
            request.InternalTargetId,
            request.CollectionEnabled,
            request.PublicReadEnabled,
            request.ExpectedRevision,
            request.Reason,
            changedByUserId);
    }

    public static LiveOperationsDashboardDto ToHttp(this LiveOperationsDashboardResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new LiveOperationsDashboardDto
        {
            SourceId = result.SourceId,
            SourceDisplayName = result.SourceDisplayName,
            ConfiguredCollectionEnabled = result.ConfiguredCollectionEnabled,
            ConfiguredPublicReadEnabled = result.ConfiguredPublicReadEnabled,
            AdapterVersion = result.AdapterVersion,
            TransformationVersion = result.TransformationVersion,
            UsagePolicyVersion = result.UsagePolicyVersion,
            TermsUrl = result.TermsUrl,
            UsagePolicyReviewedAtUtc = result.UsagePolicyReviewedAtUtc,
            AttributionText = result.AttributionText,
            AttributionUrl = result.AttributionUrl,
            Polling = result.Polling.ToHttp(),
            Summary = result.Summary.ToHttp(),
            Scopes = result.Scopes.Select(static scope => scope.ToHttp()).ToArray(),
            GeneratedAtUtc = result.GeneratedAtUtc,
        };
    }

    public static LiveOperationalScopeDto ToHttp(this LiveOperationalScopeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new LiveOperationalScopeDto
        {
            ScopeType = (LiveOperationalScopeTypeDto)result.ScopeType,
            SourceId = result.SourceId,
            ExternalEntityId = result.ExternalEntityId,
            InternalParkId = result.InternalParkId,
            TargetType = result.TargetType.HasValue
                ? (LiveTargetTypeDto)result.TargetType.Value
                : null,
            InternalTargetId = result.InternalTargetId,
            DisplayName = result.DisplayName,
            ParentDisplayName = result.ParentDisplayName,
            CollectionEnabled = result.CollectionEnabled,
            PublicReadEnabled = result.PublicReadEnabled,
            EffectiveCollectionEnabled = result.EffectiveCollectionEnabled,
            EffectivePublicReadEnabled = result.EffectivePublicReadEnabled,
            Revision = result.Revision,
            Reason = result.Reason,
            ChangedByUserId = result.ChangedByUserId,
            RecordedAtUtc = result.RecordedAtUtc,
        };
    }

    private static LiveOperationsPollingDto ToHttp(this LiveOperationsPollingResult result)
    {
        return new LiveOperationsPollingDto
        {
            ExternalEntityId = result.ExternalEntityId,
            NextAttemptAtUtc = result.NextAttemptAtUtc,
            LastPolledAtUtc = result.LastPolledAtUtc,
            LastSuccessfulPollAtUtc = result.LastSuccessfulPollAtUtc,
            ConsecutiveFailures = result.ConsecutiveFailures,
            CircuitOpenUntilUtc = result.CircuitOpenUntilUtc,
            LastDisposition = result.LastDisposition?.ToString(),
            LeaseActive = result.LeaseActive,
            LeaseExpiresAtUtc = result.LeaseExpiresAtUtc,
        };
    }

    private static LiveOperationsSummaryDto ToHttp(this LiveOperationsSummaryResult result)
    {
        return new LiveOperationsSummaryDto
        {
            MappingCount = result.MappingCount,
            EligibleMappingCount = result.EligibleMappingCount,
            CandidateMappingCount = result.CandidateMappingCount,
            SuspendedMappingCount = result.SuspendedMappingCount,
            PendingIncidentCount = result.PendingIncidentCount,
        };
    }
}
