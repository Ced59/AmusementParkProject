using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record CompleteVisitCommand(
    string UserId,
    string VisitId,
    long ExpectedVersion) : ICommand<ApplicationResult<VisitResult>>;
