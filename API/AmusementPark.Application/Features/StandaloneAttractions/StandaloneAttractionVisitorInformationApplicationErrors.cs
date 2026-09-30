using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.StandaloneAttractions;

internal static class StandaloneAttractionVisitorInformationApplicationErrors
{
    public static ApplicationError AttractionNotFound()
    {
        return ApplicationError.NotFound(
            "standalone-attraction.visitor-information.not-found",
            "L'attraction autonome est introuvable.");
    }

    public static ApplicationError OpeningHoursNotFound()
    {
        return ApplicationError.NotFound(
            "standalone-attraction.opening-hours.not-found",
            "Les horaires de l'attraction autonome sont introuvables.");
    }

    public static ApplicationError PricingNotFound()
    {
        return ApplicationError.NotFound(
            "standalone-attraction.pricing.not-found",
            "Les tarifs de l'attraction autonome sont introuvables.");
    }

    public static ApplicationError CurrentInformationNotAllowed()
    {
        return ApplicationError.RuleViolation(
            "standalone-attraction.visitor-information.closed",
            "Les informations actuelles ne peuvent pas être configurées pour une attraction définitivement fermée.");
    }
}
