using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public enum DurableBackgroundJobHandlerOutcome
{
    Succeeded,
    Retry,
    DeadLetter,
}
