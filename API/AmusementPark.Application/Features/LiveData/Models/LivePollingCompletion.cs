using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LivePollingCompletion
{
    public LivePollingCompletion(
        LivePollingLease lease,
        LivePollingCompletionDisposition disposition,
        DateTime completedAtUtc,
        DateTime nextAttemptAtUtc,
        int consecutiveFailures,
        DateTime? circuitOpenUntilUtc,
        DateTime? lastSuccessfulPollAtUtc,
        bool replaceEntityTag,
        string? entityTag)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (!Enum.IsDefined(disposition))
        {
            throw new ArgumentOutOfRangeException(nameof(disposition));
        }

        ValidateUtc(completedAtUtc, nameof(completedAtUtc));
        ValidateUtc(nextAttemptAtUtc, nameof(nextAttemptAtUtc));
        ValidateOptionalUtc(circuitOpenUntilUtc, nameof(circuitOpenUntilUtc));
        ValidateOptionalUtc(lastSuccessfulPollAtUtc, nameof(lastSuccessfulPollAtUtc));
        if (consecutiveFailures < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consecutiveFailures));
        }

        if (!LiveProviderEntityTag.TryNormalize(entityTag, out string? normalizedEntityTag))
        {
            throw new ArgumentException("The polling entity tag is invalid.", nameof(entityTag));
        }

        this.Lease = lease;
        this.Disposition = disposition;
        this.CompletedAtUtc = completedAtUtc;
        this.NextAttemptAtUtc = nextAttemptAtUtc;
        this.ConsecutiveFailures = consecutiveFailures;
        this.CircuitOpenUntilUtc = circuitOpenUntilUtc;
        this.LastSuccessfulPollAtUtc = lastSuccessfulPollAtUtc;
        this.ReplaceEntityTag = replaceEntityTag;
        this.EntityTag = normalizedEntityTag;
    }

    public LivePollingLease Lease { get; }

    public LivePollingCompletionDisposition Disposition { get; }

    public DateTime CompletedAtUtc { get; }

    public DateTime NextAttemptAtUtc { get; }

    public int ConsecutiveFailures { get; }

    public DateTime? CircuitOpenUntilUtc { get; }

    public DateTime? LastSuccessfulPollAtUtc { get; }

    public bool ReplaceEntityTag { get; }

    public string? EntityTag { get; }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A polling completion timestamp must be UTC.", parameterName);
        }
    }

    private static void ValidateOptionalUtc(DateTime? value, string parameterName)
    {
        if (value is not null)
        {
            ValidateUtc(value.Value, parameterName);
        }
    }
}
