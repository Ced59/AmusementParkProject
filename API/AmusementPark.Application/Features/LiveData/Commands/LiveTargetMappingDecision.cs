namespace AmusementPark.Application.Features.LiveData.Commands;

public enum LiveTargetMappingDecision
{
    Verify = 1,
    Correct = 2,
    Suspend = 3,
    Supersede = 4,
    Reject = 5,
}
