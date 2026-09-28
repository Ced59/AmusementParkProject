using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LivePollingTarget
{
    public LivePollingTarget(
        LiveDataSourceId sourceId,
        string externalEntityId,
        LivePollingActiveWindow activeWindow,
        LivePollingPolicy policy,
        TimeSpan maximumJitter)
    {
        _ = sourceId.Value;
        string normalizedExternalEntityId = IdentifierRules.NormalizeRequired(
            externalEntityId,
            nameof(externalEntityId));
        if (normalizedExternalEntityId is "." or "..")
        {
            throw new ArgumentException(
                "A polling target cannot use a relative path segment.",
                nameof(externalEntityId));
        }

        ArgumentNullException.ThrowIfNull(activeWindow);
        ArgumentNullException.ThrowIfNull(policy);
        if (maximumJitter < TimeSpan.Zero || maximumJitter > policy.PollingInterval)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumJitter));
        }

        this.SourceId = sourceId;
        this.ExternalEntityId = normalizedExternalEntityId;
        this.ActiveWindow = activeWindow;
        this.Policy = policy;
        this.MaximumJitter = maximumJitter;
    }

    public LiveDataSourceId SourceId { get; }

    public string ExternalEntityId { get; }

    public LivePollingActiveWindow ActiveWindow { get; }

    public LivePollingPolicy Policy { get; }

    public TimeSpan MaximumJitter { get; }
}
