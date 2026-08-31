using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public enum DurableBackgroundJobWorkload
{
    Light,
    Heavy,
}

public enum DurableBackgroundJobHandlerOutcome
{
    Succeeded,
    Retry,
    DeadLetter,
}

public enum DurableBackgroundJobExecutionDisposition
{
    Completed,
    RetryScheduled,
    DeadLettered,
    RevisionReplayQueued,
    Cancelled,
    LeaseLost,
    TransitionFailed,
}

public sealed class DurableBackgroundJobHandlerDefinition
{
    private const int MaximumKindLength = 200;
    private const int MaximumAttemptsLimit = 100;
    private const int MaximumConcurrencyLimit = 16;
    private static readonly TimeSpan MaximumTimeout = TimeSpan.FromDays(1);
    private static readonly TimeSpan MaximumRetryDelayLimit = TimeSpan.FromDays(1);

    public DurableBackgroundJobHandlerDefinition(
        string kind,
        DurableBackgroundJobWorkload workload,
        IReadOnlyCollection<int> supportedPayloadVersions,
        TimeSpan timeout,
        int maximumAttempts,
        TimeSpan initialRetryDelay,
        TimeSpan maximumRetryDelay,
        int maximumConcurrency = 1)
    {
        string normalizedKind = kind?.Trim() ?? string.Empty;
        if (normalizedKind.Length == 0 || normalizedKind.Length > MaximumKindLength)
        {
            throw new ArgumentException(
                $"The kind must contain between 1 and {MaximumKindLength} characters.",
                nameof(kind));
        }

        if (!Enum.IsDefined(workload))
        {
            throw new ArgumentOutOfRangeException(nameof(workload));
        }

        ArgumentNullException.ThrowIfNull(supportedPayloadVersions);
        int[] normalizedVersions = supportedPayloadVersions
            .Distinct()
            .OrderBy(static version => version)
            .ToArray();
        if (normalizedVersions.Length == 0 || normalizedVersions.Any(static version => version <= 0))
        {
            throw new ArgumentException(
                "At least one strictly positive payload version is required.",
                nameof(supportedPayloadVersions));
        }

        if (timeout <= TimeSpan.Zero || timeout > MaximumTimeout)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        if (maximumAttempts <= 0 || maximumAttempts > MaximumAttemptsLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        }

        if (initialRetryDelay <= TimeSpan.Zero || initialRetryDelay > MaximumRetryDelayLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(initialRetryDelay));
        }

        if (maximumRetryDelay < initialRetryDelay || maximumRetryDelay > MaximumRetryDelayLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumRetryDelay));
        }

        if (maximumConcurrency <= 0 || maximumConcurrency > MaximumConcurrencyLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrency));
        }

        this.Kind = normalizedKind;
        this.Workload = workload;
        this.SupportedPayloadVersions = Array.AsReadOnly(normalizedVersions);
        this.Timeout = timeout;
        this.MaximumAttempts = maximumAttempts;
        this.InitialRetryDelay = initialRetryDelay;
        this.MaximumRetryDelay = maximumRetryDelay;
        this.MaximumConcurrency = maximumConcurrency;
    }

    public string Kind { get; }

    public DurableBackgroundJobWorkload Workload { get; }

    public IReadOnlyCollection<int> SupportedPayloadVersions { get; }

    public TimeSpan Timeout { get; }

    public int MaximumAttempts { get; }

    public TimeSpan InitialRetryDelay { get; }

    public TimeSpan MaximumRetryDelay { get; }

    public int MaximumConcurrency { get; }

    public bool SupportsPayloadVersion(int payloadVersion)
    {
        return this.SupportedPayloadVersions.Contains(payloadVersion);
    }
}

public sealed record DurableBackgroundJobExecutionContext(
    string JobId,
    int PayloadVersion,
    JsonElement Payload,
    long? RequestedRevision,
    int AttemptCount,
    string? CorrelationId);

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

public sealed record DurableBackgroundJobExecutionResult(
    DurableBackgroundJobExecutionDisposition Disposition,
    DurableBackgroundJobStatus? PersistedStatus = null,
    string? ErrorCode = null);

public static class DurableBackgroundJobErrorCodes
{
    public const string UnknownKind = "background-job.unknown-kind";
    public const string UnsupportedPayloadVersion = "background-job.unsupported-payload-version";
    public const string InvalidHandlerResult = "background-job.invalid-handler-result";
    public const string HandlerCancelled = "background-job.handler-cancelled";
    public const string HandlerTimeout = "background-job.handler-timeout";
    public const string AttemptBudgetExhausted = "background-job.attempt-budget-exhausted";
    public const string UnhandledException = "background-job.unhandled-exception";
}
