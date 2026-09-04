namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class PassportStatisticsContentFence
{
    public static bool AllowsRead(
        long? currentFenceToken,
        long? stableFenceToken,
        bool fenceReady,
        long? occurrenceFenceToken)
    {
        if (!currentFenceToken.HasValue)
        {
            return !occurrenceFenceToken.HasValue;
        }

        long current = currentFenceToken.Value;
        if (fenceReady)
        {
            return occurrenceFenceToken == current;
        }

        if (stableFenceToken.HasValue)
        {
            return occurrenceFenceToken >= stableFenceToken.Value
                && occurrenceFenceToken <= current;
        }

        return !occurrenceFenceToken.HasValue
            || occurrenceFenceToken is >= 1
                && occurrenceFenceToken <= current;
    }
}
