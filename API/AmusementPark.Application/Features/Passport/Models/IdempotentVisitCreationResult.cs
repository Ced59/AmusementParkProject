using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record IdempotentVisitCreationResult(
    IdempotentVisitCreationStatus Status,
    Visit? Visit);
