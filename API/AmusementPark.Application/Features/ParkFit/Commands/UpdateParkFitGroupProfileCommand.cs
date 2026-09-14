using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Results;

namespace AmusementPark.Application.Features.ParkFit.Commands;

public sealed record UpdateParkFitGroupProfileCommand(
    string OwnerUserId,
    string ProfileId,
    long ExpectedVersion,
    ParkFitGroupProfileInput Profile)
    : ICommand<ApplicationResult<ParkFitGroupProfileResult>>;
