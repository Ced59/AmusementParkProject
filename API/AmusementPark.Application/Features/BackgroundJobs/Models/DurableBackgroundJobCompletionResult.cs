using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record DurableBackgroundJobCompletionResult(
    string JobId,
    DurableBackgroundJobStatus Status,
    long? RequestedRevision,
    long? ProcessedRevision);
