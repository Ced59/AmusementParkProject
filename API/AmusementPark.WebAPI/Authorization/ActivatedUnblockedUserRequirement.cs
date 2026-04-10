using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Authorization;

/// <summary>
/// Requirement vérifiant que l'utilisateur authentifié est activé et non bloqué.
/// </summary>
public sealed class ActivatedUnblockedUserRequirement : IAuthorizationRequirement
{
    public static ActivatedUnblockedUserRequirement Instance { get; } = new ActivatedUnblockedUserRequirement();

    private ActivatedUnblockedUserRequirement()
    {
    }
}
