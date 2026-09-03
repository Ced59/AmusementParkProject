using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record UserVisitListCursor(
    VisitDate Date,
    DateTime UpdatedAtUtc,
    VisitId VisitId);

public sealed record UserVisitListCriteria(
    string UserId,
    int Limit,
    string? ParkId = null,
    int? Year = null,
    VisitStatus? Status = null,
    UserVisitListCursor? After = null)
{
    public const int DefaultLimit = 25;

    public const int MaximumLimit = 100;
}

public sealed record UserVisitPage(
    IReadOnlyCollection<Visit> Items,
    UserVisitListCursor? NextCursor);

public enum IdempotentVisitCreationStatus
{
    Created = 1,
    Replayed = 2,
    Conflict = 3,
}

public sealed record IdempotentVisitCreationResult(
    IdempotentVisitCreationStatus Status,
    Visit? Visit);
