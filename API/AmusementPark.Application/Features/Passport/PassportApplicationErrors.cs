using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport;

public static class PassportApplicationErrors
{
    public static ApplicationError InvalidDate(
        string code,
        string message,
        string? parameterName = null)
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? details =
            string.IsNullOrWhiteSpace(parameterName)
                ? null
                : new Dictionary<string, IReadOnlyCollection<string>>
                {
                    [parameterName] = new[] { code },
                };
        return ApplicationError.Validation(code, message, details);
    }

    public static ApplicationError InvalidVisit(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError InvalidIdentifier(
        string code,
        string message,
        string? parameterName)
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? details =
            string.IsNullOrWhiteSpace(parameterName)
                ? null
                : new Dictionary<string, IReadOnlyCollection<string>>
                {
                    [parameterName] = new[] { code },
                };
        return ApplicationError.Validation(code, message, details);
    }

    public static ApplicationError InvalidTimeZone()
    {
        return ApplicationError.Validation(
            "visit.time-zone-id-invalid",
            "Le fuseau horaire de la visite est invalide.",
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["TimeZoneId"] = new[] { "invalid-time-zone" },
            });
    }

    public static ApplicationError InvalidIdempotencyKey()
    {
        return ApplicationError.Validation(
            "visit.idempotency-key-invalid",
            "La clé d'idempotence doit contenir entre 1 et 128 caractères affichables.",
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["Idempotency-Key"] = new[] { "invalid" },
            });
    }

    public static ApplicationError IdempotencyConflict()
    {
        return ApplicationError.Conflict(
            "visit.idempotency-key-conflict",
            "Cette clé d'idempotence a déjà été utilisée avec un contenu différent.");
    }

    public static ApplicationError ParkNotFound()
    {
        return ApplicationError.NotFound(
            "visit.park-not-found",
            "Le parc associé à la visite est introuvable.");
    }

    public static ApplicationError VisitNotFound()
    {
        return ApplicationError.NotFound(
            "visit.not-found",
            "La visite demandée est introuvable.");
    }

    public static ApplicationError InvalidVisitVersion()
    {
        return ApplicationError.Validation(
            "visit.version-invalid",
            "La version de la visite doit être positive.");
    }

    public static ApplicationError VisitConcurrencyConflict()
    {
        return ApplicationError.Conflict(
            "visit.version-conflict",
            "La visite a changé. Recharge-la avant de réessayer.");
    }

    public static ApplicationError VisitNotEditable()
    {
        return ApplicationError.Conflict(
            "visit.not-editable",
            "Rouvre la visite avant de modifier son contenu.");
    }

    public static ApplicationError VisitTemporalMetadataLocked()
    {
        return ApplicationError.Conflict(
            "visit.temporal-metadata-locked",
            "La date, le fuseau et la convention de journée ne peuvent pas changer tant que la visite contient des occurrences.");
    }

    public static ApplicationError InvalidListLimit()
    {
        return ApplicationError.Validation(
            "visit.list-limit-invalid",
            $"La taille de page doit être comprise entre 1 et {UserVisitListCriteria.MaximumLimit}.");
    }

    public static ApplicationError InvalidYear()
    {
        return ApplicationError.Validation(
            "visit.list-year-invalid",
            "L'année de filtrage est invalide.");
    }

    public static ApplicationError InvalidStatus()
    {
        return ApplicationError.Validation(
            "visit.list-status-invalid",
            "Le statut de filtrage est invalide.");
    }

    public static ApplicationError InvalidRideOccurrence(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError InvalidVisitParkAssessment(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError InvalidVisitParkAssessmentVersion()
    {
        return ApplicationError.Validation(
            "visit-park-assessment.version-invalid",
            "La version de la visite doit être positive.");
    }

    public static ApplicationError InvalidRideAssessment(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError InvalidRideAssessmentVersion()
    {
        return ApplicationError.Validation(
            "ride-assessment.version-invalid",
            "La version de l’occurrence doit être positive.");
    }

    public static ApplicationError RideAssessmentConcurrencyConflict()
    {
        return ApplicationError.Conflict(
            "ride-assessment.version-conflict",
            "L’occurrence ou son évaluation a changé. Recharge le journal avant de réessayer.");
    }

    public static ApplicationError VisitParkAssessmentConcurrencyConflict()
    {
        return ApplicationError.Conflict(
            "visit-park-assessment.version-conflict",
            "La visite ou son évaluation a changé. Recharge la visite avant de réessayer.");
    }

    public static ApplicationError InvalidRideOccurrenceBatch()
    {
        return ApplicationError.Validation(
            "ride-occurrence.batch-invalid",
            "Le lot doit créer entre 1 et 100 occurrences valides.");
    }

    public static ApplicationError InvalidRideOccurrenceListLimit()
    {
        return ApplicationError.Validation(
            "ride-occurrence.list-limit-invalid",
            $"La taille de page doit être comprise entre 1 et {RideOccurrenceListCriteria.MaximumLimit}.");
    }

    public static ApplicationError RideOccurrenceNotFound()
    {
        return ApplicationError.NotFound(
            "ride-occurrence.not-found",
            "L’occurrence demandée est introuvable.");
    }

    public static ApplicationError VisitTargetNotFound()
    {
        return ApplicationError.NotFound(
            "ride-occurrence.target-not-found",
            "L’attraction associée à l’occurrence est introuvable.");
    }

    public static ApplicationError VisitTargetParkMismatch()
    {
        return ApplicationError.Validation(
            "ride-occurrence.target-park-mismatch",
            "L’attraction n’appartient pas au parc de cette visite.");
    }

    public static ApplicationError VisitTargetNotAttraction()
    {
        return ApplicationError.Validation(
            "ride-occurrence.target-not-attraction",
            "Seules les attractions peuvent être ajoutées au journal des tours.");
    }

    public static ApplicationError HistoricalConflictConfirmationRequired()
    {
        return ApplicationError.Conflict(
            "ride-occurrence.historical-conflict-confirmation-required",
            "Les dates connues de l’attraction sont incompatibles avec la visite. Une confirmation explicite est requise.");
    }

    public static ApplicationError RideOccurrenceConcurrencyConflict()
    {
        return ApplicationError.Conflict(
            "ride-occurrence.version-conflict",
            "L’occurrence ou son ordre a changé. Recharge le journal avant de réessayer.");
    }

    public static ApplicationError GlobalRatingSuggestionTargetNotFound()
    {
        return ApplicationError.NotFound(
            "passport.rating-suggestion-target-not-found",
            "La note globale associée à cette suggestion est introuvable.");
    }

    public static ApplicationError InvalidGlobalRatingSuggestionInteraction()
    {
        return ApplicationError.Validation(
            "passport.rating-suggestion-interaction-invalid",
            "L’interaction de suggestion demandée est invalide.");
    }

    public static ApplicationError RideOccurrenceIdempotencyConflict()
    {
        return ApplicationError.Conflict(
            "ride-occurrence.idempotency-key-conflict",
            "Cette clé d’idempotence a déjà été utilisée avec un contenu différent.");
    }

    public static ApplicationError InvalidRideOccurrenceReorder()
    {
        return ApplicationError.Validation(
            "ride-occurrence.reorder-invalid",
            "Le déplacement demandé est invalide.");
    }

    public static ApplicationError InvalidRideOccurrenceUpdate()
    {
        return ApplicationError.Validation(
            "ride-occurrence.update-invalid",
            "Les données de correction de l’occurrence sont invalides.");
    }
}
