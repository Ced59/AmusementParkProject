using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record DurableBackgroundJobLease(
    string JobId,
    string LeaseOwner,
    string LeaseToken);
