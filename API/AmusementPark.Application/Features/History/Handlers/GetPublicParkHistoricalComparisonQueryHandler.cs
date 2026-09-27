using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetPublicParkHistoricalComparisonQueryHandler :
    IQueryHandler<GetPublicParkHistoricalComparisonQuery, ApplicationResult<PublicParkHistoricalComparisonResult>>
{
    private readonly PublicParkHistoricalDataLoader dataLoader;
    private readonly IParkHistoricalSnapshotBuilder snapshotBuilder;
    private readonly IParkHistoricalComparisonBuilder comparisonBuilder;

    public GetPublicParkHistoricalComparisonQueryHandler(
        PublicParkHistoricalDataLoader dataLoader,
        IParkHistoricalSnapshotBuilder snapshotBuilder,
        IParkHistoricalComparisonBuilder comparisonBuilder)
    {
        this.dataLoader = dataLoader;
        this.snapshotBuilder = snapshotBuilder;
        this.comparisonBuilder = comparisonBuilder;
    }

    public async Task<ApplicationResult<PublicParkHistoricalComparisonResult>> HandleAsync(
        GetPublicParkHistoricalComparisonQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<PublicParkHistoricalComparisonResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        if (!TryBuildInstants(query.FromYear, query.ToYear, out HistoricalInstant? from, out HistoricalInstant? to))
        {
            return ApplicationResult<PublicParkHistoricalComparisonResult>.Failure(
                HistoryApplicationErrors.InvalidComparisonRange());
        }

        string parkId = query.ParkId.Trim();
        PublicParkHistoricalData? data = await this.dataLoader.LoadAsync(parkId, cancellationToken);
        if (data is null)
        {
            return ApplicationResult<PublicParkHistoricalComparisonResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), parkId));
        }

        ParkHistoricalSnapshot fromSnapshot = this.snapshotBuilder.Build(
            data.Park.Id,
            from!,
            data.Subjects,
            data.Facts);
        ParkHistoricalSnapshot toSnapshot = this.snapshotBuilder.Build(
            data.Park.Id,
            to!,
            data.Subjects,
            data.Facts);
        ParkHistoricalComparison comparison = this.comparisonBuilder.Build(fromSnapshot, toSnapshot);
        return ApplicationResult<PublicParkHistoricalComparisonResult>.Success(
            new PublicParkHistoricalComparisonResult(
                data.Park,
                comparison,
                data.Facts,
                data.ZoneNames));
    }

    private static bool TryBuildInstants(
        int fromYear,
        int toYear,
        out HistoricalInstant? from,
        out HistoricalInstant? to)
    {
        from = null;
        to = null;
        if (fromYear >= toYear)
        {
            return false;
        }

        try
        {
            from = HistoricalInstant.ForYear(fromYear);
            to = HistoricalInstant.ForYear(toYear);
            return true;
        }
        catch (HistoricalTemporalValidationException)
        {
            return false;
        }
    }
}
