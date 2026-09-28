using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LivePollingLeaseRequest
{
    public LivePollingLeaseRequest(
        LiveDataSourceId sourceId,
        string externalEntityId,
        string leaseOwner,
        DateTime nowUtc,
        TimeSpan leaseDuration,
        TimeSpan crashRecoveryCooldown)
    {
        _ = sourceId.Value;
        string normalizedExternalEntityId = IdentifierRules.NormalizeRequired(
            externalEntityId,
            nameof(externalEntityId));
        string normalizedLeaseOwner = IdentifierRules.NormalizeRequired(
            leaseOwner,
            nameof(leaseOwner));

        if (nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The polling lease timestamp must be UTC.", nameof(nowUtc));
        }

        if (leaseDuration <= TimeSpan.Zero || leaseDuration >= LivePollingPolicy.MinimumPollingInterval)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        if (crashRecoveryCooldown < LivePollingPolicy.MinimumPollingInterval
            || crashRecoveryCooldown > LivePollingPolicy.MaximumDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(crashRecoveryCooldown));
        }

        this.SourceId = sourceId;
        this.ExternalEntityId = normalizedExternalEntityId;
        this.LeaseOwner = normalizedLeaseOwner;
        this.NowUtc = nowUtc;
        this.LeaseDuration = leaseDuration;
        this.CrashRecoveryCooldown = crashRecoveryCooldown;
    }

    public LiveDataSourceId SourceId { get; }

    public string ExternalEntityId { get; }

    public string LeaseOwner { get; }

    public DateTime NowUtc { get; }

    public TimeSpan LeaseDuration { get; }

    public TimeSpan CrashRecoveryCooldown { get; }
}
