using System.Globalization;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Countries.Services;

/// <summary>
/// Service applicatif réutilisable pour les conversions pays/régions.
/// </summary>
public sealed class CountryReferenceService : ICountryReferenceService
{
    private static readonly IReadOnlyCollection<string> EuropeCountryCodes = BuildCodes(
        "AD", "AL", "AT", "AX", "BA", "BE", "BG", "BY", "CH", "CY", "CZ", "DE", "DK", "EE", "ES", "FI", "FO", "FR", "GB", "GG", "GI", "GR", "HR", "HU", "IE", "IM", "IS", "IT", "JE", "LI", "LT", "LU", "LV", "MC", "MD", "ME", "MK", "MT", "NL", "NO", "PL", "PT", "RO", "RS", "SE", "SI", "SJ", "SK", "SM", "UA", "VA", "XK");

    private static readonly IReadOnlyCollection<string> NorthAmericaCountryCodes = BuildCodes(
        "AG", "AI", "AW", "BB", "BL", "BM", "BQ", "BS", "BZ", "CA", "CR", "CU", "CW", "DM", "DO", "GD", "GL", "GP", "GT", "HN", "HT", "JM", "KN", "KY", "LC", "MF", "MQ", "MS", "MX", "NI", "PA", "PM", "PR", "SV", "SX", "TC", "TT", "US", "VC", "VG", "VI");

    private static readonly IReadOnlyCollection<string> SouthAmericaCountryCodes = BuildCodes(
        "AR", "BO", "BR", "CL", "CO", "EC", "FK", "GF", "GY", "PE", "PY", "SR", "UY", "VE");

    private static readonly IReadOnlyCollection<string> AsiaCountryCodes = BuildCodes(
        "AF", "AM", "AZ", "BD", "BN", "BT", "CN", "GE", "HK", "ID", "IN", "IO", "JP", "KG", "KH", "KP", "KR", "KZ", "LA", "LK", "MM", "MN", "MO", "MV", "MY", "NP", "PH", "PK", "RU", "SG", "TH", "TJ", "TL", "TM", "TW", "UZ", "VN");

    private static readonly IReadOnlyCollection<string> MiddleEastCountryCodes = BuildCodes(
        "AE", "BH", "IL", "IQ", "IR", "JO", "KW", "LB", "OM", "PS", "QA", "SA", "SY", "TR", "YE");

    private static readonly IReadOnlyCollection<string> OceaniaCountryCodes = BuildCodes(
        "AS", "AU", "CC", "CK", "CX", "FJ", "FM", "GU", "HM", "KI", "MH", "MP", "NC", "NF", "NR", "NU", "NZ", "PF", "PG", "PN", "PW", "SB", "TK", "TO", "TV", "UM", "VU", "WF", "WS");

    private static readonly IReadOnlyCollection<string> AfricaCountryCodes = BuildCodes(
        "AO", "BF", "BI", "BJ", "BW", "CD", "CF", "CG", "CI", "CM", "CV", "DJ", "DZ", "EG", "EH", "ER", "ET", "GA", "GH", "GM", "GN", "GQ", "GW", "KE", "KM", "LR", "LS", "LY", "MA", "MG", "ML", "MR", "MU", "MW", "MZ", "NA", "NE", "NG", "RE", "RW", "SC", "SD", "SH", "SL", "SN", "SO", "SS", "ST", "SZ", "TD", "TG", "TN", "TZ", "UG", "YT", "ZA", "ZM", "ZW");

    private static readonly IReadOnlyDictionary<WorldRegionFilter, IReadOnlyCollection<string>> CountryCodesByRegion = new Dictionary<WorldRegionFilter, IReadOnlyCollection<string>>
    {
        [WorldRegionFilter.Europe] = EuropeCountryCodes,
        [WorldRegionFilter.NorthAmerica] = NorthAmericaCountryCodes,
        [WorldRegionFilter.SouthAmerica] = SouthAmericaCountryCodes,
        [WorldRegionFilter.Asia] = AsiaCountryCodes,
        [WorldRegionFilter.MiddleEast] = MiddleEastCountryCodes,
        [WorldRegionFilter.Oceania] = OceaniaCountryCodes,
        [WorldRegionFilter.Orient] = MergeCodes(AsiaCountryCodes, MiddleEastCountryCodes, OceaniaCountryCodes),
        [WorldRegionFilter.Africa] = AfricaCountryCodes,
    };

    private readonly ICountryReadRepository countryReadRepository;

    public CountryReferenceService(ICountryReadRepository countryReadRepository)
    {
        this.countryReadRepository = countryReadRepository;
    }

    public async Task<IReadOnlyCollection<string>> FindCountryCodesByLocalizedSearchAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        string normalizedTerm = (searchTerm ?? string.Empty).Trim();
        if (normalizedTerm.Length == 0)
        {
            return Array.Empty<string>();
        }

        IReadOnlyCollection<Country> countries = await this.countryReadRepository.GetAllAsync(null, cancellationToken);
        List<string> matchingCodes = new List<string>();

        foreach (Country country in countries)
        {
            string isoCode = NormalizeCountryCode(country.IsoCode);
            if (isoCode.Length == 0)
            {
                continue;
            }

            if (ContainsInsensitive(isoCode, normalizedTerm) || country.Names.Any(name => MatchesLocalizedName(name, normalizedTerm)))
            {
                matchingCodes.Add(isoCode);
            }
        }

        return matchingCodes
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyCollection<string> GetCountryCodesForRegion(WorldRegionFilter? region)
    {
        if (region is null || !CountryCodesByRegion.TryGetValue(region.Value, out IReadOnlyCollection<string>? countryCodes))
        {
            return Array.Empty<string>();
        }

        return countryCodes;
    }

    private static bool MatchesLocalizedName(LocalizedText localizedText, string searchTerm)
    {
        string value = localizedText.Value?.Trim() ?? string.Empty;
        return value.Length > 0 && ContainsInsensitive(value, searchTerm);
    }

    private static bool ContainsInsensitive(string value, string searchTerm)
    {
        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            value,
            searchTerm,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }

    private static string NormalizeCountryCode(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static IReadOnlyCollection<string> BuildCodes(params string[] countryCodes)
    {
        return countryCodes
            .Select(static code => NormalizeCountryCode(code))
            .Where(static code => code.Length == 2)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyCollection<string> MergeCodes(params IReadOnlyCollection<string>[] codeGroups)
    {
        return codeGroups
            .SelectMany(static countryCodes => countryCodes)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
