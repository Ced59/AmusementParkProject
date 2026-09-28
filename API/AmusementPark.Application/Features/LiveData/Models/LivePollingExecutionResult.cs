namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LivePollingExecutionResult
{
    public LivePollingExecutionResult(
        LivePollingExecutionDisposition disposition,
        int observationCount = 0,
        int diagnosticCount = 0,
        bool circuitOpened = false)
    {
        if (!Enum.IsDefined(disposition))
        {
            throw new ArgumentOutOfRangeException(nameof(disposition));
        }

        if (observationCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(observationCount));
        }

        if (diagnosticCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(diagnosticCount));
        }

        this.Disposition = disposition;
        this.ObservationCount = observationCount;
        this.DiagnosticCount = diagnosticCount;
        this.CircuitOpened = circuitOpened;
    }

    public LivePollingExecutionDisposition Disposition { get; }

    public int ObservationCount { get; }

    public int DiagnosticCount { get; }

    public bool CircuitOpened { get; }
}
