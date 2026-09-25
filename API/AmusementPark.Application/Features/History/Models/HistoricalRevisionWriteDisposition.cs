namespace AmusementPark.Application.Features.History.Models;

public enum HistoricalRevisionWriteDisposition
{
    Created = 0,
    AlreadyExists = 1,
    Conflict = 2,
}
