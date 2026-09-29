namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveOperationalControlPolicy
{
    public bool AllowsCollection(
        bool configuredEnabled,
        IEnumerable<LiveOperationalControl> controls,
        string externalEntityId,
        string? internalParkId = null,
        LiveTargetType? targetType = null,
        string? internalTargetId = null)
    {
        return configuredEnabled && Evaluate(
            controls,
            externalEntityId,
            internalParkId,
            targetType,
            internalTargetId,
            static control => control.CollectionEnabled);
    }

    public bool AllowsPublicRead(
        bool configuredEnabled,
        IEnumerable<LiveOperationalControl> controls,
        string externalEntityId,
        string? internalParkId = null,
        LiveTargetType? targetType = null,
        string? internalTargetId = null)
    {
        return configuredEnabled && Evaluate(
            controls,
            externalEntityId,
            internalParkId,
            targetType,
            internalTargetId,
            static control => control.PublicReadEnabled);
    }

    private static bool Evaluate(
        IEnumerable<LiveOperationalControl> controls,
        string externalEntityId,
        string? internalParkId,
        LiveTargetType? targetType,
        string? internalTargetId,
        Func<LiveOperationalControl, bool> selector)
    {
        ArgumentNullException.ThrowIfNull(controls);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalEntityId);
        return controls
            .Where(control => control.Scope.AppliesTo(
                externalEntityId,
                internalParkId,
                targetType,
                internalTargetId))
            .All(selector);
    }
}
