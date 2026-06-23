using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.TechnicalStats;

public static class TechnicalStatsApplicationErrors
{
    public static ApplicationError Unavailable()
    {
        return ApplicationError.Technical(
            "technical-stats.unavailable",
            "Technical statistics are not available.");
    }
}
