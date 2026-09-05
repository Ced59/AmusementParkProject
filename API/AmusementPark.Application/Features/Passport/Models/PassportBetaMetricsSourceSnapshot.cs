namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportBetaMetricsSourceSnapshot(
    long CreatedVisits,
    long CompletedVisits,
    long UsersWithCompletedVisit,
    long UsersWithSecondCompletedVisit,
    IReadOnlyCollection<PassportBetaDailyMetrics> Daily);
