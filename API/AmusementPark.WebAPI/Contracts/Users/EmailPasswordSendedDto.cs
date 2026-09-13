namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après demande d'oubli de mot de passe.
/// </summary>
public sealed class EmailPasswordSendedDto
{
    public string Message { get; set; } = string.Empty;
}
