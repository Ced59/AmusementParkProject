using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Agrégat de lecture pré-calculé pour les moyennes et classements publics.
/// </summary>
public sealed class RatingAggregate : AuditableEntity
{
    public RatingTargetType TargetType { get; set; }

    public string TargetId { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public ParkItemCategory? ParkItemCategory { get; set; }

    public ParkItemType? ParkItemType { get; set; }

    public long RatingCount { get; set; }

    public double RatingSum { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }

    public DateTime? LastRatedAtUtc { get; set; }

    public long? MutationVersion { get; set; }

    public long? CalculatedVersion { get; set; }

    public bool? IsCalculationCurrent
    {
        get
        {
            if (!this.MutationVersion.HasValue || !this.CalculatedVersion.HasValue)
            {
                return null;
            }

            return IsCalculationCurrentForVersions(
                this.MutationVersion.Value,
                this.CalculatedVersion.Value);
        }
    }

    public static bool IsCalculationCurrentForVersions(long mutationVersion, long calculatedVersion)
    {
        return mutationVersion >= 0
            && calculatedVersion >= mutationVersion;
    }
}
