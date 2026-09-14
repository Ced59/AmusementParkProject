namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Synthèse datée des filtres durs sans perdre le nombre de faits inconnus.
/// </summary>
public sealed class ParkFitHardFilterEvaluation
{
    public ParkFitHardFilterEvaluation(
        DateOnly evaluationDate,
        int evaluatedFilterCount,
        int failedFilterCount,
        int unknownFilterCount)
    {
        if (evaluatedFilterCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(evaluatedFilterCount));
        }

        if (failedFilterCount < 0 || failedFilterCount > evaluatedFilterCount)
        {
            throw new ArgumentOutOfRangeException(nameof(failedFilterCount));
        }

        if (unknownFilterCount < 0
            || unknownFilterCount > evaluatedFilterCount - failedFilterCount)
        {
            throw new ArgumentOutOfRangeException(nameof(unknownFilterCount));
        }

        this.EvaluationDate = evaluationDate;
        this.EvaluatedFilterCount = evaluatedFilterCount;
        this.FailedFilterCount = failedFilterCount;
        this.UnknownFilterCount = unknownFilterCount;
        this.State = failedFilterCount > 0
            ? ParkFitHardFilterState.Failed
            : unknownFilterCount > 0
                ? ParkFitHardFilterState.Unknown
                : ParkFitHardFilterState.Passed;
    }

    public DateOnly EvaluationDate { get; }

    public int EvaluatedFilterCount { get; }

    public int FailedFilterCount { get; }

    public int UnknownFilterCount { get; }

    public ParkFitHardFilterState State { get; }
}
