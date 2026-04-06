using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.CaptainCoaster.Contracts;
using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Commands;

/// <summary>
/// Lance un import Captain Coaster.
/// </summary>
public sealed record StartCaptainCoasterImportCommand(CaptainCoasterSourceDescriptor Source) : ICommand<ApplicationResult<CaptainCoasterSessionResult>>;
