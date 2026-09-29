using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LiveOperationalGateSnapshot
{
    private readonly IReadOnlyCollection<LiveOperationalControl> controls;
    private readonly LiveOperationalControlPolicy policy;

    public LiveOperationalGateSnapshot(
        bool configuredCollectionEnabled,
        bool configuredPublicReadEnabled,
        string externalEntityId,
        IReadOnlyCollection<LiveOperationalControl> controls,
        LiveOperationalControlPolicy policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalEntityId);
        ArgumentNullException.ThrowIfNull(controls);
        ArgumentNullException.ThrowIfNull(policy);
        this.ConfiguredCollectionEnabled = configuredCollectionEnabled;
        this.ConfiguredPublicReadEnabled = configuredPublicReadEnabled;
        this.ExternalEntityId = externalEntityId;
        this.controls = controls;
        this.policy = policy;
    }

    public bool ConfiguredCollectionEnabled { get; }

    public bool ConfiguredPublicReadEnabled { get; }

    public string ExternalEntityId { get; }

    public IReadOnlyCollection<LiveOperationalControl> Controls => this.controls;

    public LiveOperationalGateSnapshot WithControl(LiveOperationalControl control)
    {
        ArgumentNullException.ThrowIfNull(control);
        LiveOperationalControl[] updatedControls = this.controls
            .Where(existing => !existing.Scope.Equals(control.Scope))
            .Append(control)
            .ToArray();
        return new LiveOperationalGateSnapshot(
            this.ConfiguredCollectionEnabled,
            this.ConfiguredPublicReadEnabled,
            this.ExternalEntityId,
            updatedControls,
            this.policy);
    }

    public bool AllowsCollection(
        string? parkId = null,
        LiveTargetType? targetType = null,
        string? targetId = null)
    {
        return this.policy.AllowsCollection(
            this.ConfiguredCollectionEnabled,
            this.controls,
            this.ExternalEntityId,
            parkId,
            targetType,
            targetId);
    }

    public bool AllowsPublicRead(
        string? parkId = null,
        LiveTargetType? targetType = null,
        string? targetId = null)
    {
        return this.policy.AllowsPublicRead(
            this.ConfiguredPublicReadEnabled,
            this.controls,
            this.ExternalEntityId,
            parkId,
            targetType,
            targetId);
    }
}
