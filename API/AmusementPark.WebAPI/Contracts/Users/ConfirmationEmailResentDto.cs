namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après renvoi d'email de confirmation.
/// </summary>
public sealed class ConfirmationEmailResentDto
{
    public string Message { get; set; } = string.Empty;
}
