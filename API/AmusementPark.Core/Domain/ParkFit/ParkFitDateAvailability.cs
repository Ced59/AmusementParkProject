namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Disponibilité d'un parc liée à la date exacte qui a été évaluée.
/// </summary>
public sealed class ParkFitDateAvailability
{
    public ParkFitDateAvailability(
        ParkFitDateAvailabilityState state,
        DateOnly evaluationDate)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        this.State = state;
        this.EvaluationDate = evaluationDate;
    }

    public ParkFitDateAvailabilityState State { get; }

    public DateOnly EvaluationDate { get; }
}
