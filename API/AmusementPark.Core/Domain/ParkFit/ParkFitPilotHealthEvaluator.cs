namespace AmusementPark.Core.Domain.ParkFit;

public static class ParkFitPilotHealthEvaluator
{
    private const decimal AttentionThresholdPercent = 25m;

    public static ParkFitPilotHealth Evaluate(
        long searchesStarted,
        long searchesCompleted,
        long noResultSearches,
        long significantUnknownSearches,
        long explanationsViewed,
        long comparisonsOpened)
    {
        decimal completionRate = Percentage(searchesCompleted, searchesStarted);
        decimal noResultRate = Percentage(noResultSearches, searchesCompleted);
        decimal significantUnknownRate = Percentage(
            significantUnknownSearches,
            searchesCompleted);
        decimal explanationRate = Percentage(explanationsViewed, searchesCompleted);
        decimal comparisonRate = Percentage(comparisonsOpened, searchesCompleted);
        ParkFitPilotSignal signal = searchesCompleted == 0
            ? ParkFitPilotSignal.AwaitingObservations
            : noResultRate >= AttentionThresholdPercent
                || significantUnknownRate >= AttentionThresholdPercent
                    ? ParkFitPilotSignal.NeedsAttention
                    : explanationsViewed > 0 || comparisonsOpened > 0
                        ? ParkFitPilotSignal.Encouraging
                        : ParkFitPilotSignal.Monitor;

        return new ParkFitPilotHealth(
            completionRate,
            noResultRate,
            significantUnknownRate,
            explanationRate,
            comparisonRate,
            signal,
            true);
    }

    private static decimal Percentage(long numerator, long denominator)
    {
        if (numerator <= 0 || denominator <= 0)
        {
            return 0m;
        }

        return Math.Round(
            Math.Min(100m, numerator * 100m / denominator),
            1,
            MidpointRounding.AwayFromZero);
    }
}
