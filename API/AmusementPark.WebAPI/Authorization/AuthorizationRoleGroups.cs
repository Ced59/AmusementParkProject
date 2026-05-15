namespace AmusementPark.WebAPI.Authorization;

/// <summary>
/// Groupes de rôles réutilisables dans les attributs d'autorisation HTTP.
/// </summary>
public static class AuthorizationRoleGroups
{
    public const string UserModeratorAdmin = "USER,MODERATOR,ADMIN";

    public const string ModeratorAdmin = "MODERATOR,ADMIN";

    public const string Admin = "ADMIN";
}
