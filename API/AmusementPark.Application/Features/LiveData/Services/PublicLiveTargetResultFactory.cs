using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class PublicLiveTargetResultFactory
{
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly LiveLatestObservationSelectionPolicy selectionPolicy;

    public PublicLiveTargetResultFactory(
        ILiveDataSourceCatalog sourceCatalog,
        LiveLatestObservationSelectionPolicy selectionPolicy)
    {
        this.sourceCatalog = sourceCatalog;
        this.selectionPolicy = selectionPolicy;
    }

    public PublicLiveTargetResult Create(
        string targetId,
        LiveTargetType targetType,
        string displayName,
        string parkId,
        string parkDisplayName,
        IReadOnlyCollection<LiveLatestObservation> observations,
        DateTime asOfUtc)
    {
        Dictionary<LiveDataSourceId, LiveDataSourcePresentation> presentations = observations
            .Select(static observation => observation.Provenance.SourceId)
            .Distinct()
            .Select(sourceId => this.sourceCatalog.Find(sourceId))
            .Where(static presentation => presentation?.Source.Status == LiveDataSourceStatus.Active)
            .Cast<LiveDataSourcePresentation>()
            .ToDictionary(static presentation => presentation.Source.Id);
        Dictionary<LiveDataSourceId, int> priorities = presentations.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.Priority);
        LiveLatestObservation? selected = this.selectionPolicy.Select(
            observations,
            priorities,
            asOfUtc);
        if (selected is null)
        {
            return BuildMissingTarget(
                targetId,
                targetType,
                displayName,
                parkId,
                parkDisplayName,
                asOfUtc);
        }

        LiveFreshnessAssessment freshness = selected.FreshnessPolicy.Assess(
            selected.Provenance.ObservedAtUtc,
            asOfUtc);
        PublicLiveAvailability availability = freshness.State switch
        {
            LiveFreshnessState.Expired => PublicLiveAvailability.Expired,
            LiveFreshnessState.Unavailable => PublicLiveAvailability.Unavailable,
            _ => PublicLiveAvailability.Current,
        };
        bool exposeCurrentFacts = availability == PublicLiveAvailability.Current;
        LiveDataSourcePresentation presentation = presentations[selected.Provenance.SourceId];
        IReadOnlyCollection<PublicLiveQueueResult> queues = exposeCurrentFacts
            ? selected.Queues.Select(ToPublicQueue).ToList().AsReadOnly()
            : Array.Empty<PublicLiveQueueResult>();
        return new PublicLiveTargetResult(
            targetId,
            targetType,
            displayName,
            parkId,
            parkDisplayName,
            availability,
            exposeCurrentFacts ? selected.Status : null,
            queues,
            asOfUtc,
            selected.Provenance.ObservedAtUtc,
            selected.Provenance.ReceivedAtUtc,
            freshness.Age.HasValue ? checked((long)freshness.Age.Value.TotalSeconds) : null,
            freshness.State,
            freshness.ExpiresAtUtc,
            new PublicLiveSourceResult(
                presentation.Source.Id.Value,
                presentation.Source.DisplayName,
                presentation.Source.Type,
                presentation.AttributionText,
                presentation.AttributionUrl),
            selected.Provenance.Confidence);
    }

    private static PublicLiveTargetResult BuildMissingTarget(
        string targetId,
        LiveTargetType targetType,
        string displayName,
        string parkId,
        string parkDisplayName,
        DateTime asOfUtc)
    {
        return new PublicLiveTargetResult(
            targetId,
            targetType,
            displayName,
            parkId,
            parkDisplayName,
            PublicLiveAvailability.NoObservation,
            null,
            Array.Empty<PublicLiveQueueResult>(),
            asOfUtc,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    private static PublicLiveQueueResult ToPublicQueue(LiveQueueObservation queue)
    {
        return new PublicLiveQueueResult(
            queue.Kind,
            queue.WaitTimeMinutes,
            queue.IsEstimated,
            queue.Availability,
            queue.ReturnStartUtc,
            queue.ReturnEndUtc,
            queue.CurrentGroupStart,
            queue.CurrentGroupEnd,
            queue.NextAllocationUtc,
            queue.PriceMinorUnits,
            queue.CurrencyCode);
    }
}
