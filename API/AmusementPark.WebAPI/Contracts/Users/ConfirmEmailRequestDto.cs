namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de confirmation d'email.
/// </summary>
public sealed class ConfirmEmailRequestDto
{
    public string Token { get; set; } = string.Empty;
}
