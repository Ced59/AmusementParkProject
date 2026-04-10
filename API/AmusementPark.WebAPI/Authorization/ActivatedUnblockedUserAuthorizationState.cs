namespace AmusementPark.WebAPI.Authorization;

/// <summary>
/// Codes internes de refus pour la policy d'utilisateur actif et non bloqué.
/// </summary>
public static class ActivatedUnblockedUserAuthorizationState
{
    public const string HttpContextItemKey = "Authorization:ActivatedUnblockedUser:Failure";
    public const string MissingUserId = "missing-user-id";
    public const string UserNotFound = "user-not-found";
    public const string UserNotActivated = "user-not-activated";
    public const string UserBlocked = "user-blocked";
}
