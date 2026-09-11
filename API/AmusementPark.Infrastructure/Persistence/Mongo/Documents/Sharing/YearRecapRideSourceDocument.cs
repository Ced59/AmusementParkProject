using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

internal sealed class YearRecapRideSourceDocument
{
    public string Id { get; init; } = string.Empty;

    public string VisitId { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public string ParkItemId { get; init; } = string.Empty;

    public RideOccurrenceStatus Status { get; init; }

    public byte? AssessmentValueHalfSteps { get; init; }

    public string? HistoricalName { get; init; }

    public string? HistoricalCategory { get; init; }

    public long Version { get; init; }

    public long? ContentMutationFenceToken { get; init; }
}
