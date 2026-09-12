using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record LeaseBackgroundJobRequest(
    IReadOnlyCollection<string> Kinds,
    string LeaseOwner,
    TimeSpan LeaseDuration);
