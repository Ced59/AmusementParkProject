namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de confirmation d'email.
/// </summary>
public sealed class ConfirmEmailRequestDto
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP retourné après confirmation d'email.
/// </summary>
public sealed class EmailConfirmedDto
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP de demande de renvoi d'email de confirmation.
/// </summary>
public sealed class ResendConfirmationEmailDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP retourné après renvoi d'email de confirmation.
/// </summary>
public sealed class ConfirmationEmailResentDto
{
    public string Message { get; set; } = string.Empty;
}
