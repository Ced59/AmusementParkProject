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
}
