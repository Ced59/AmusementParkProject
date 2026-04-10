namespace AmusementPark.Infrastructure.Configuration.Initialization;

/// <summary>
/// Paramètres de seed de l'utilisateur administrateur local.
/// </summary>
public sealed class AdminSeedSettings
{
    public bool Enabled { get; set; } = true;

    public string Email { get; set; } = "c.caudron59@gmail.com";

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = "Ced";

    public string LastName { get; set; } = "Caudron";

    public string PreferredLanguage { get; set; } = "FR";
}
