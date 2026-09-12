using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record DurableBackgroundJobStateTransitionResult(
    string JobId,
    DurableBackgroundJobStatus Status,
    long? RequestedRevision,
    long? ProcessedRevision);
