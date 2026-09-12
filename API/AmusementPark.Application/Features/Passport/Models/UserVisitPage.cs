using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record UserVisitPage(
    IReadOnlyCollection<Visit> Items,
    UserVisitListCursor? NextCursor);
