namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après confirmation d'email.
/// </summary>
public sealed class EmailConfirmedDto
{
    public string Message { get; set; } = string.Empty;
}
