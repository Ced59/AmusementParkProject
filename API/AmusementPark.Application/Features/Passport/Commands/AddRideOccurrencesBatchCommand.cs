using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record AddRideOccurrencesBatchCommand(
    string UserId,
    string VisitId,
    string ClientOperationId,
    IReadOnlyCollection<RideOccurrenceCreationItem?> Items,
    RideLogSource Source = RideLogSource.Manual)
    : ICommand<ApplicationResult<CreateRideOccurrencesResult>>;
