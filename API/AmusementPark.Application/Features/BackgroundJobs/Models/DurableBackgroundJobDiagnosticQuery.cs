using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record DurableBackgroundJobDiagnosticQuery(
    IReadOnlyCollection<DurableBackgroundJobStatus>? Statuses = null,
    string? Kind = null,
    int Limit = 100,
    string? NaturalKey = null,
    long? ProcessedRevision = null,
    DateTime? MaximumCreatedAtUtc = null);
