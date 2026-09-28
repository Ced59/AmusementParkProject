using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LiveProviderReadResult
{
    public LiveProviderReadResult(
        LiveProviderReadDisposition disposition,
        DateTime receivedAtUtc,
        IReadOnlyCollection<ExternalLiveObservation>? observations = null,
        IReadOnlyCollection<LiveProviderDiagnostic>? diagnostics = null,
        string? entityTag = null,
        TimeSpan? retryAfter = null,
        string? payloadSha256 = null)
    {
        if (!Enum.IsDefined(disposition))
        {
            throw new ArgumentOutOfRangeException(nameof(disposition));
        }

        if (receivedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The provider reception timestamp must be UTC.", nameof(receivedAtUtc));
        }

        if (retryAfter < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAfter));
        }

        if (!LiveProviderEntityTag.TryNormalize(entityTag, out string? normalizedEntityTag))
        {
            throw new ArgumentException(
                "A provider entity tag must be a concrete strong or weak HTTP entity tag.",
                nameof(entityTag));
        }

        string? normalizedPayloadSha256 = NormalizeHash(payloadSha256);
        List<ExternalLiveObservation> normalizedObservations = observations?.ToList()
            ?? new List<ExternalLiveObservation>();
        List<LiveProviderDiagnostic> normalizedDiagnostics = diagnostics?.ToList()
            ?? new List<LiveProviderDiagnostic>();
        if (normalizedObservations.Any(static observation => observation is null)
            || normalizedDiagnostics.Any(static diagnostic => diagnostic is null))
        {
            throw new ArgumentException("A provider result cannot contain empty entries.");
        }

        this.Disposition = disposition;
        this.ReceivedAtUtc = receivedAtUtc;
        this.Observations = normalizedObservations.AsReadOnly();
        this.Diagnostics = normalizedDiagnostics.AsReadOnly();
        this.EntityTag = normalizedEntityTag;
        this.RetryAfter = retryAfter;
        this.PayloadSha256 = normalizedPayloadSha256;
    }

    public LiveProviderReadDisposition Disposition { get; }

    public DateTime ReceivedAtUtc { get; }

    public IReadOnlyCollection<ExternalLiveObservation> Observations { get; }

    public IReadOnlyCollection<LiveProviderDiagnostic> Diagnostics { get; }

    public string? EntityTag { get; }

    public TimeSpan? RetryAfter { get; }

    public string? PayloadSha256 { get; }

    private static string? NormalizeHash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim().ToLowerInvariant();
        if (normalizedValue.Length != 64
            || normalizedValue.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("A provider payload hash must be a SHA-256 hexadecimal value.", nameof(value));
        }

        return normalizedValue;
    }
}
