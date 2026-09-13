namespace AmusementPark.Application.Features.Sharing.Services;

public enum ShareModerationDecisionExecutionOutcome
{
    Succeeded = 0,
    RetryableConflict = 1,
    ReportNotFound = 2,
    TargetNotFound = 3,
    InvalidTransition = 4,
}
