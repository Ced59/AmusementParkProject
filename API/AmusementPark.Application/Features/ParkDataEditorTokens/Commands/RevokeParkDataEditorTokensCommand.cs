using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Commands;

public sealed record RevokeParkDataEditorTokensCommand(
    string UserId,
    string? TokenId,
    string RevokedByUserId,
    string Reason) : ICommand<ApplicationResult<RevokedParkDataEditorTokensResult>>;
