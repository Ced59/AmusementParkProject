using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkFit;

/// <summary>
/// Erreurs stables du moteur public Park FIT.
/// </summary>
public static class ParkFitApplicationErrors
{
    public static ApplicationError InvalidSearch(string field, string message)
    {
        return ApplicationError.Validation(
            "park-fit.search.invalid",
            message,
            new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
            {
                [field] = new[] { message },
            });
    }
}
