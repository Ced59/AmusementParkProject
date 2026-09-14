using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class ParkFitOperationalStatusMongoMapper
{
    public static ParkFitOperationalStatusDocument ToDocument(this ParkFitOperationalStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return new ParkFitOperationalStatusDocument
        {
            Id = status.ParkId,
            State = status.State,
            Revision = status.Revision,
            Decisions = status.Decisions.Select(static decision =>
                new ParkFitOperationalDecisionDocument
                {
                    Type = decision.Type,
                    ActorUserId = decision.ActorUserId,
                    Reason = decision.Reason,
                    DecidedAtUtc = decision.DecidedAtUtc,
                    Revision = decision.Revision,
                }).ToList(),
            CreatedAt = status.Decisions.FirstOrDefault()?.DecidedAtUtc
                ?? status.UpdatedAtUtc
                ?? DateTime.UtcNow,
            UpdatedAt = status.UpdatedAtUtc ?? DateTime.UtcNow,
        };
    }

    public static ParkFitOperationalStatus ToDomain(this ParkFitOperationalStatusDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ParkFitOperationalStatus.Restore(
            document.Id,
            document.State,
            document.Revision,
            document.Revision == 0 ? null : document.UpdatedAt,
            document.Decisions
                .OrderBy(static decision => decision.Revision)
                .Select(static decision => new ParkFitOperationalDecision(
                    decision.Type,
                    decision.ActorUserId,
                    decision.Reason,
                    decision.DecidedAtUtc,
                    decision.Revision)));
    }
}
