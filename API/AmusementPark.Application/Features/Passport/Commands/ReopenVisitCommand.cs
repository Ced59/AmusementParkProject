using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record ReopenVisitCommand(
    string UserId,
    string VisitId,
    long ExpectedVersion) : ICommand<ApplicationResult<VisitResult>>;
