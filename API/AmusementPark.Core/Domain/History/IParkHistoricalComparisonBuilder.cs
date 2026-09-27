namespace AmusementPark.Core.Domain.History;

public interface IParkHistoricalComparisonBuilder
{
    ParkHistoricalComparison Build(
        ParkHistoricalSnapshot from,
        ParkHistoricalSnapshot to);
}
