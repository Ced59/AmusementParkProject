namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveOperationsPollingDto
{
    public string ExternalEntityId { get; set; } = string.Empty;

    public DateTime? NextAttemptAtUtc { get; set; }

    public DateTime? LastPolledAtUtc { get; set; }

    public DateTime? LastSuccessfulPollAtUtc { get; set; }

    public int ConsecutiveFailures { get; set; }

    public DateTime? CircuitOpenUntilUtc { get; set; }

    public string? LastDisposition { get; set; }

    public bool LeaseActive { get; set; }

    public DateTime? LeaseExpiresAtUtc { get; set; }
}
