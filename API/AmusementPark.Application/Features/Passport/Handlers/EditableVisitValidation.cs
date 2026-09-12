using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

internal sealed record EditableVisitValidation(
    Visit? Visit,
    ApplicationError? Error);
