namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitHistoryExclusions
{
    public LiveWaitHistoryExclusions(
        int duplicateObservations,
        int outsideActiveWindow,
        int nonOperatingStatus,
        int missingStandbyWait)
    {
        if (duplicateObservations < 0
            || outsideActiveWindow < 0
            || nonOperatingStatus < 0
            || missingStandbyWait < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duplicateObservations));
        }

        this.DuplicateObservations = duplicateObservations;
        this.OutsideActiveWindow = outsideActiveWindow;
        this.NonOperatingStatus = nonOperatingStatus;
        this.MissingStandbyWait = missingStandbyWait;
    }

    public int DuplicateObservations { get; }

    public int OutsideActiveWindow { get; }

    public int NonOperatingStatus { get; }

    public int MissingStandbyWait { get; }

    public int Total => checked(
        this.DuplicateObservations
        + this.OutsideActiveWindow
        + this.NonOperatingStatus
        + this.MissingStandbyWait);
}
