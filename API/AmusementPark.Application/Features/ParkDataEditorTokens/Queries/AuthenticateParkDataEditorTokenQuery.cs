using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Queries;

public sealed record AuthenticateParkDataEditorTokenQuery(
    string PlainTextToken) : IQuery<ApplicationResult<ParkDataEditorTokenAuthenticationResult>>;
