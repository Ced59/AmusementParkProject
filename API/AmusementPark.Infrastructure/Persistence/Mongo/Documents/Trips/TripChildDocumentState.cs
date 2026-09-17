namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

public enum TripChildDocumentState
{
    Reserved = 1,
    Committed = 2,
    Deleted = 3,
}
