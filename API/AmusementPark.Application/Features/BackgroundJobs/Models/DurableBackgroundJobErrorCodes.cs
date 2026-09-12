using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

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
