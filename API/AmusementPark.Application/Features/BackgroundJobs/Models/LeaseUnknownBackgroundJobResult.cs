using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record LeaseUnknownBackgroundJobResult(
    DurableBackgroundJob? Job,
    string? NextAfterKind);
