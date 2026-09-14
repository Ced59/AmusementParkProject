namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Distance datée, sans la présenter comme un trajet routier.
/// </summary>
public sealed class ParkFitTravelDistance
{
    public ParkFitTravelDistance(
        double distanceKilometers,
        ParkFitDistanceMethod method,
        DateTime evaluatedAtUtc)
    {
        if (!double.IsFinite(distanceKilometers) || distanceKilometers < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceKilometers));
        }

        if (!Enum.IsDefined(method))
        {
            throw new ArgumentOutOfRangeException(nameof(method));
        }

        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The distance evaluation timestamp must use UTC.",
                nameof(evaluatedAtUtc));
        }

        this.DistanceKilometers = Math.Round(
            distanceKilometers,
            1,
            MidpointRounding.AwayFromZero);
        this.Method = method;
        this.EvaluatedAtUtc = evaluatedAtUtc;
    }

    public double DistanceKilometers { get; }

    public ParkFitDistanceMethod Method { get; }

    public DateTime EvaluatedAtUtc { get; }
}
