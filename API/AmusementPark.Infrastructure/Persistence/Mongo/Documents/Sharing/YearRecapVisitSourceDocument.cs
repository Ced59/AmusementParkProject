using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

internal sealed class YearRecapVisitSourceDocument
{
    public string Id { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public VisitDateDocument Date { get; init; } = new VisitDateDocument();

    public byte? ParkAssessmentValueHalfSteps { get; init; }

    public long Version { get; init; }

    public string? ContentMutationLeaseToken { get; init; }

    public long? ContentMutationFenceToken { get; init; }

    public long? ContentMutationFenceStableToken { get; init; }

    public bool ContentMutationFenceReady { get; init; }
}
