namespace AmusementPark.Application.Features.ParkFit.Models;

public sealed record ParkFitPilotMetricsSnapshot(
    IReadOnlyCollection<ParkFitPilotDailyMetrics> Daily);
