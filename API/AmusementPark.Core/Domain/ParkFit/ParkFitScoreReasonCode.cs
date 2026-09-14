namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Codes stables traduisibles qui expliquent le score global ou son absence.
/// </summary>
public enum ParkFitScoreReasonCode
{
    ScoreAvailable,
    HardFilterFailed,
    DateUnavailable,
    CriticalDataExcluded,
    CriticalDataSuspended,
    NoKnownSubscore,
    DataConfidenceUnknown,
    IncompleteCoverageCapApplied,
    DataConfidenceCapApplied,
    CriticalUnknownCapApplied,
    OptionalSubscoreNotApplicable,
}
