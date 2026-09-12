using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record DurableBackgroundJobHandlerResult
{
    private const int MaximumErrorCodeLength = 200;

    private DurableBackgroundJobHandlerResult(DurableBackgroundJobHandlerOutcome outcome, string? errorCode)
    {
        this.Outcome = outcome;
        this.ErrorCode = errorCode;
    }

    public DurableBackgroundJobHandlerOutcome Outcome { get; }

    public string? ErrorCode { get; }

    public static DurableBackgroundJobHandlerResult Success()
    {
        return new DurableBackgroundJobHandlerResult(DurableBackgroundJobHandlerOutcome.Succeeded, null);
    }

    public static DurableBackgroundJobHandlerResult Retry(string errorCode)
    {
        return new DurableBackgroundJobHandlerResult(
            DurableBackgroundJobHandlerOutcome.Retry,
            NormalizeErrorCode(errorCode));
    }

    public static DurableBackgroundJobHandlerResult DeadLetter(string errorCode)
    {
        return new DurableBackgroundJobHandlerResult(
            DurableBackgroundJobHandlerOutcome.DeadLetter,
            NormalizeErrorCode(errorCode));
    }

    private static string NormalizeErrorCode(string errorCode)
    {
        string normalized = errorCode?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > MaximumErrorCodeLength)
        {
            throw new ArgumentException(
                $"The error code must contain between 1 and {MaximumErrorCodeLength} characters.",
                nameof(errorCode));
        }

        return normalized;
    }
}
