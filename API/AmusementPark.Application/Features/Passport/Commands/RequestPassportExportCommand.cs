using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record RequestPassportExportCommand(
    string UserId,
    PassportExportFormat Format) : ICommand<ApplicationResult<PassportExport>>;
