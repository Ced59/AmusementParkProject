namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Réunit le fait de distance et sa composante de score explicable.
/// </summary>
public sealed class ParkFitTravelEvaluation
{
    public ParkFitTravelEvaluation(
        ParkFitSubscore subscore,
        ParkFitTravelDistance? distance)
    {
        ArgumentNullException.ThrowIfNull(subscore);

        if (subscore.Kind != ParkFitSubscoreKind.TravelConvenience)
        {
            throw new ArgumentException(
                "The travel evaluation requires a travel-convenience subscore.",
                nameof(subscore));
        }

        this.Subscore = subscore;
        this.Distance = distance;
    }

    public ParkFitSubscore Subscore { get; }

    public ParkFitTravelDistance? Distance { get; }
}
