using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

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
