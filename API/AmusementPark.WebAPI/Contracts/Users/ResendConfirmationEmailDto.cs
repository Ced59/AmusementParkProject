namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de demande de renvoi d'email de confirmation.
/// </summary>
public sealed class ResendConfirmationEmailDto
{
    public string Email { get; set; } = string.Empty;
}
