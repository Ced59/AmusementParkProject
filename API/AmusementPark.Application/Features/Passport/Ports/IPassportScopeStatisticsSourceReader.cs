namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Lit les projections privées minimales nécessaires aux statistiques d'un parc ou d'une année.
/// </summary>
public interface IPassportScopeStatisticsSourceReader
{
    Task<PassportGlobalStatisticsSource> ReadGlobalAsync(
        string userId,
        int? year,
        string? parkId,
        CancellationToken cancellationToken);

    Task<PassportParkStatisticsSource> ReadParkAsync(
        string userId,
        string parkId,
        CancellationToken cancellationToken);

    Task<PassportYearStatisticsSource> ReadYearAsync(
        string userId,
        int year,
        CancellationToken cancellationToken);
}
