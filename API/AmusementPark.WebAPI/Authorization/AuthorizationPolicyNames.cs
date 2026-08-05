namespace AmusementPark.WebAPI.Authorization;

/// <summary>
/// Noms des policies d'autorisation WebAPI.
/// </summary>
public static class AuthorizationPolicyNames
{
    public const string ActivatedUnblockedUser = nameof(ActivatedUnblockedUser);

    public const string AdminOrParkDataEditorToken = nameof(AdminOrParkDataEditorToken);

    public const string ParkDataEditorToken = nameof(ParkDataEditorToken);

    public const string ParkDataEditorJwt = nameof(ParkDataEditorJwt);
}
