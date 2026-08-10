using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkPricing;

public static class ParkPricingApplicationErrors
{
    public static ApplicationError ParkNotFound()
    {
        return ApplicationError.NotFound("park-pricing.park-not-found", "Le parc est introuvable.");
    }

    public static ApplicationError PricingNotFound()
    {
        return ApplicationError.NotFound("park-pricing.not-found", "Les tarifs du parc sont introuvables.");
    }

    public static ApplicationError PricingNotAllowed(ParkStatus status)
    {
        return ApplicationError.Validation(
            "park-pricing.not-operating",
            "Les tarifs actuels ne peuvent être configurés que pour un parc en activité.",
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["parkStatus"] = new[] { $"Le statut '{status}' n'autorise pas les tarifs actuels." },
            });
    }

    public static ApplicationError InvalidPricing(IReadOnlyDictionary<string, IReadOnlyCollection<string>> details)
    {
        return ApplicationError.Validation("park-pricing.invalid", "Les tarifs du parc sont invalides.", details);
    }
}
