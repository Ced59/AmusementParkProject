namespace AmusementPark.WebAPI.Contracts.Countries;

/// <summary>
/// Contrat HTTP retourné pour un pays.
/// </summary>
public sealed class CountryDto
{
    /// <summary>
    /// Code ISO alpha-2.
    /// </summary>
    public string IsoCode { get; set; } = string.Empty;

    /// <summary>
    /// Nom localisé du pays.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
