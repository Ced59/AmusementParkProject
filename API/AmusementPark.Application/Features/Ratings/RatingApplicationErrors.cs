using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Ratings;

public static class RatingApplicationErrors
{
    public static ApplicationError InvalidTargetType()
    {
        return ApplicationError.Validation(
            "rating.target-type.invalid",
            "La cible de note est invalide.");
    }

    public static ApplicationError InvalidRatingValue()
    {
        return ApplicationError.Validation(
            "rating.value.invalid",
            "La note doit être comprise entre 0,5 et 5, par palier de 0,5.");
    }

    public static ApplicationError TargetNotFound()
    {
        return ApplicationError.NotFound(
            "rating.target.not-found",
            "La cible à noter est introuvable.");
    }
}
