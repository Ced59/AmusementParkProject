using AmusementPark.Application.Features.BackgroundJobs.Models;

namespace AmusementPark.Application.Features.FactualEvents.Services;

public sealed class FactualChangeTerminalSchedulingException : Exception
{
    public FactualChangeTerminalSchedulingException(
        DurableBackgroundJobStatus status,
        string? errorCode)
        : base($"The factual materialization job is terminal ({status}, {errorCode ?? "unknown"}).")
    {
        this.Status = status;
        this.ErrorCode = errorCode;
    }

    public DurableBackgroundJobStatus Status { get; }

    public string? ErrorCode { get; }
}
