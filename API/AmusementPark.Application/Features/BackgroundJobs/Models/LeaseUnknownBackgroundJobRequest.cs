using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record LeaseUnknownBackgroundJobRequest(
    IReadOnlyCollection<string> KnownKinds,
    string LeaseOwner,
    TimeSpan LeaseDuration,
    TimeSpan MinimumAge,
    int MaximumCandidateDocuments,
    string? AfterKind = null);
