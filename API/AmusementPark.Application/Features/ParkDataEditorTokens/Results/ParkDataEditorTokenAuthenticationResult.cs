using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Results;

public sealed record ParkDataEditorTokenAuthenticationResult(
    User User,
    ParkDataEditorAccessToken Token);
