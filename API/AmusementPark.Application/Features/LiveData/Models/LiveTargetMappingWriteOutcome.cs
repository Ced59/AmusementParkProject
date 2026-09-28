namespace AmusementPark.Application.Features.LiveData.Models;

public enum LiveTargetMappingWriteOutcome
{
    Created = 1,
    AlreadyExists = 2,
    Conflict = 3,
}
