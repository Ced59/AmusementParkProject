using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetPublicParkHistoricalSnapshotQueryHandler :
    IQueryHandler<GetPublicParkHistoricalSnapshotQuery, ApplicationResult<PublicParkHistoricalSnapshotResult>>
{
    private readonly PublicParkHistoricalDataLoader dataLoader;
    private readonly IParkHistoricalSnapshotBuilder snapshotBuilder;

    public GetPublicParkHistoricalSnapshotQueryHandler(
        PublicParkHistoricalDataLoader dataLoader,
        IParkHistoricalSnapshotBuilder snapshotBuilder)
    {
        this.dataLoader = dataLoader;
        this.snapshotBuilder = snapshotBuilder;
    }

    public async Task<ApplicationResult<PublicParkHistoricalSnapshotResult>> HandleAsync(
        GetPublicParkHistoricalSnapshotQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<PublicParkHistoricalSnapshotResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        HistoricalInstant? instant = BuildInstant(query.Year, query.Month, query.Day);
        if (instant is null)
        {
            return ApplicationResult<PublicParkHistoricalSnapshotResult>.Failure(
                HistoryApplicationErrors.InvalidSnapshotDate());
        }

        string parkId = query.ParkId.Trim();
        PublicParkHistoricalData? data = await this.dataLoader.LoadAsync(parkId, cancellationToken);
        if (data is null)
        {
            return ApplicationResult<PublicParkHistoricalSnapshotResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), parkId));
        }

        ParkHistoricalSnapshot snapshot = this.snapshotBuilder.Build(
            data.Park.Id,
            instant,
            data.Subjects,
            data.Facts);
        return ApplicationResult<PublicParkHistoricalSnapshotResult>.Success(
            new PublicParkHistoricalSnapshotResult(
                data.Park,
                snapshot,
                data.Facts,
                data.ZoneNames));
    }

    private static HistoricalInstant? BuildInstant(int year, int? month, int? day)
    {
        try
        {
            if (day.HasValue)
            {
                return month.HasValue
                    ? HistoricalInstant.ForDay(year, month.Value, day.Value)
                    : null;
            }

            return month.HasValue
                ? HistoricalInstant.ForMonth(year, month.Value)
                : HistoricalInstant.ForYear(year);
        }
        catch (HistoricalTemporalValidationException)
        {
            return null;
        }
    }
}
