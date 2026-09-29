using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record LiveWaitForecastBacktestResult(
    string TargetDisplayName,
    string ParkDisplayName,
    LiveWaitForecastBacktestReport Report,
    DateTime GeneratedAtUtc);
