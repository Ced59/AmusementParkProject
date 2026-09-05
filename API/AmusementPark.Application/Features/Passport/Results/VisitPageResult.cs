using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record VisitPageResult(
    IReadOnlyCollection<VisitResult> Items,
    UserVisitListCursor? NextCursor);
