using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Ratings;

public enum RankingSnapshotStatus
{
    Building,
    Validated,
    Current,
    Superseded,
    Failed,
}
