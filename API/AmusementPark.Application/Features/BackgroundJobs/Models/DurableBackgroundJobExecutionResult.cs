using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record DurableBackgroundJobExecutionResult(
    DurableBackgroundJobExecutionDisposition Disposition,
    DurableBackgroundJobStatus? PersistedStatus = null,
    string? ErrorCode = null,
    Task? OngoingHandlerCompletion = null);
