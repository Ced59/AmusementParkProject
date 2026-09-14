using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkFit.Commands;

public sealed record DeleteParkFitGroupProfileCommand(
    string OwnerUserId,
    string ProfileId,
    long ExpectedVersion)
    : ICommand<ApplicationResult>;
