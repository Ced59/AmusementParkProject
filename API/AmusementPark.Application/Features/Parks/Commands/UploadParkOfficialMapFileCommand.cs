using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.Parks.Commands;

public sealed record UploadParkOfficialMapFileCommand(ParkOfficialMapFileUploadRequest Request)
    : ICommand<ApplicationResult<ParkOfficialMapStoredFile>>;
