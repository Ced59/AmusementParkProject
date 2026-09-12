using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public enum DurableBackgroundJobStatus
{
    Pending,
    Leased,
    Succeeded,
    RetryScheduled,
    DeadLetter,
    Cancelled,
    Superseded,
}
