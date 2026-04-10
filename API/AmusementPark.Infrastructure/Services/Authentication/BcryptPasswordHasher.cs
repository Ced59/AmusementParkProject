using AmusementPark.Application.Ports;

namespace AmusementPark.Infrastructure.Services.Authentication;

/// <summary>
/// Adaptateur BCrypt pour le hashage des mots de passe.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
