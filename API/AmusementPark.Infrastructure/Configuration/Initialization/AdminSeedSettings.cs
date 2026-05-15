namespace AmusementPark.Infrastructure.Configuration.Initialization;

/// <summary>
/// Paramètres de seed de l'utilisateur administrateur local.
/// </summary>
public sealed class AdminSeedSettings
{
    public bool Enabled { get; set; } = false;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = "Admin";

    public string LastName { get; set; } = "User";

    public string PreferredLanguage { get; set; } = "FR";
}
