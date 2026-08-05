using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Commands;

public sealed record CreateParkDataEditorTokenCommand(
    string UserId,
    string Label,
    int ExpiresInDays) : ICommand<ApplicationResult<CreatedParkDataEditorTokenResult>>;
