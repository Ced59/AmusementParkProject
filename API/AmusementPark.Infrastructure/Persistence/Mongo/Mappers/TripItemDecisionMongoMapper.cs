using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class TripItemDecisionMongoMapper
{
    public static TripItemDecisionDocument ToDocument(this TripItemDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return new TripItemDecisionDocument
        {
            Id = decision.Id.Value,
            TripPlanId = decision.TripPlanId.Value,
            ParkItemId = decision.ParkItemId,
            Status = decision.Status,
            Reason = decision.Reason,
            DecidedByUserId = decision.DecidedByUserId,
            Version = decision.Version,
            DocumentState = TripChildDocumentState.Committed,
            CreatedAt = ToMongoPrecision(decision.CreatedAtUtc),
            UpdatedAt = ToMongoPrecision(decision.UpdatedAtUtc),
        };
    }

    public static TripItemDecision ToDomain(this TripItemDecisionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return TripItemDecision.Restore(
            TripItemDecisionId.Parse(document.Id),
            TripPlanId.Parse(document.TripPlanId),
            document.ParkItemId,
            document.Status,
            document.Reason,
            document.DecidedByUserId,
            document.Version,
            DateTime.SpecifyKind(document.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(document.UpdatedAt, DateTimeKind.Utc));
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
