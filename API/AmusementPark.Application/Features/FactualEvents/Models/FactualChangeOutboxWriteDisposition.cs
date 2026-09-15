namespace AmusementPark.Application.Features.FactualEvents.Models;

public enum FactualChangeOutboxWriteDisposition
{
    Created = 0,
    AlreadyRecorded = 1,
    Conflict = 2,
}
