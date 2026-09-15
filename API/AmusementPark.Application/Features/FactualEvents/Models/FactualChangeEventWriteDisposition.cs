namespace AmusementPark.Application.Features.FactualEvents.Models;

public enum FactualChangeEventWriteDisposition
{
    Created = 0,
    AlreadyExists = 1,
    Conflict = 2,
}
