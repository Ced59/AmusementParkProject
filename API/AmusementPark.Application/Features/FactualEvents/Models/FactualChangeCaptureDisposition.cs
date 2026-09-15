namespace AmusementPark.Application.Features.FactualEvents.Models;

public enum FactualChangeCaptureDisposition
{
    NoChange = 0,
    Scheduled = 1,
    AlreadyRecorded = 2,
    RecordedPendingScheduling = 3,
    Conflict = 4,
    RecordedTerminalSchedulingFailure = 5,
}
