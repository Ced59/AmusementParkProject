namespace AmusementPark.Core.Domain.LiveData;

public enum LiveWaitForecastBacktestVerdict
{
    InsufficientData = 1,
    Abandon = 2,
    EligibleForPilot = 3,
}
