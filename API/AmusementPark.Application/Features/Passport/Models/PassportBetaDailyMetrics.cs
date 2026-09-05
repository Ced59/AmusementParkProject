namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportBetaDailyMetrics(
    string Date,
    long CompletedVisits,
    long FirstVisits,
    long SecondVisits);
