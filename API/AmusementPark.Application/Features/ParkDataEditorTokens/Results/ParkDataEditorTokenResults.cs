using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Results;

public sealed record CreatedParkDataEditorTokenResult(
    ParkDataEditorAccessToken Token,
    string PlainTextToken);

public sealed record ParkDataEditorTokenAuthenticationResult(
    User User,
    ParkDataEditorAccessToken Token);

public sealed record RevokedParkDataEditorTokensResult(long RevokedCount);
