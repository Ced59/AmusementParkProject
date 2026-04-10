using AmusementPark.WebAPI.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Filters;

/// <summary>
/// Alias de compatibilité vers la policy d'autorisation "utilisateur activé et non bloqué".
/// </summary>
public sealed class RequireActivatedUnblockedUserAttribute : AuthorizeAttribute
{
    public RequireActivatedUnblockedUserAttribute()
    {
        this.Policy = AuthorizationPolicyNames.ActivatedUnblockedUser;
    }
}
