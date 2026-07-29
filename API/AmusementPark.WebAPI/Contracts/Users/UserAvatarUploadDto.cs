using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP d'upload de l'avatar de l'utilisateur connecté.
/// </summary>
public sealed class UserAvatarUploadDto
{
    public IFormFile? File { get; set; }
}
