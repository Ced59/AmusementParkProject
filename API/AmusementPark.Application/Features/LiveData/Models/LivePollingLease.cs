using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LivePollingLease
{
    public LivePollingLease(
        LiveDataSourceId sourceId,
        string externalEntityId,
        string leaseOwner,
        string leaseToken,
        string? entityTag,
        DateTime? lastSuccessfulPollAtUtc,
        int consecutiveFailures,
        DateTime? circuitOpenUntilUtc)
    {
        _ = sourceId.Value;
        string normalizedExternalEntityId = IdentifierRules.NormalizeRequired(
            externalEntityId,
            nameof(externalEntityId));
        string normalizedLeaseOwner = IdentifierRules.NormalizeRequired(
            leaseOwner,
            nameof(leaseOwner));
        string normalizedLeaseToken = IdentifierRules.NormalizeRequired(
            leaseToken,
            nameof(leaseToken));
        if (!LiveProviderEntityTag.TryNormalize(entityTag, out string? normalizedEntityTag))
        {
            throw new ArgumentException("The polling entity tag is invalid.", nameof(entityTag));
        }

        if (consecutiveFailures < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consecutiveFailures));
        }

        if (circuitOpenUntilUtc is not null && circuitOpenUntilUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The circuit timestamp must be UTC.", nameof(circuitOpenUntilUtc));
        }

        if (lastSuccessfulPollAtUtc is not null
            && lastSuccessfulPollAtUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The last successful poll timestamp must be UTC.",
                nameof(lastSuccessfulPollAtUtc));
        }

        this.SourceId = sourceId;
        this.ExternalEntityId = normalizedExternalEntityId;
        this.LeaseOwner = normalizedLeaseOwner;
        this.LeaseToken = normalizedLeaseToken;
        this.EntityTag = normalizedEntityTag;
        this.LastSuccessfulPollAtUtc = lastSuccessfulPollAtUtc;
        this.ConsecutiveFailures = consecutiveFailures;
        this.CircuitOpenUntilUtc = circuitOpenUntilUtc;
    }

    public LiveDataSourceId SourceId { get; }

    public string ExternalEntityId { get; }

    public string LeaseOwner { get; }

    public string LeaseToken { get; }

    public string? EntityTag { get; }

    public DateTime? LastSuccessfulPollAtUtc { get; }

    public int ConsecutiveFailures { get; }

    public DateTime? CircuitOpenUntilUtc { get; }
}
