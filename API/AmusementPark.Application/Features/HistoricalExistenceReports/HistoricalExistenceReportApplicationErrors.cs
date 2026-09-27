using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.HistoricalExistenceReports;

public static class HistoricalExistenceReportApplicationErrors
{
    public static ApplicationError InvalidReport()
    {
        return ApplicationError.Validation(
            "history.existence-report.invalid",
            "Le signalement historique est invalide.");
    }

    public static ApplicationError ReportNotFound()
    {
        return ApplicationError.NotFound(
            "history.existence-report.not-found",
            "Le signalement historique demandé est introuvable.");
    }

    public static ApplicationError Conflict()
    {
        return ApplicationError.Conflict(
            "history.existence-report.conflict",
            "Ce signalement existe déjà ou a changé entre-temps.");
    }

    public static ApplicationError InvalidSearch()
    {
        return ApplicationError.Validation(
            "history.existence-report.search-invalid",
            "La recherche de signalements historiques est invalide.");
    }

    public static ApplicationError InvalidTransition()
    {
        return ApplicationError.RuleViolation(
            "history.existence-report.invalid-transition",
            "Ce signalement historique a déjà été traité.");
    }
}
