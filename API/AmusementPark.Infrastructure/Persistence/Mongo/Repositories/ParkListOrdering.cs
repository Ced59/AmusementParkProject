using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkListOrdering
{
    public static SortDefinition<ParkDocument> Build(ParkAdminSortField sortField, bool sortDescending, bool includeHidden)
    {
        SortDefinitionBuilder<ParkDocument> sortBuilder = Builders<ParkDocument>.Sort;

        if (sortField == ParkAdminSortField.Name)
        {
            SortDefinition<ParkDocument> primarySort = sortDescending
                ? sortBuilder.Descending(document => document.Name)
                : sortBuilder.Ascending(document => document.Name);

            return primarySort.Ascending(document => document.Id);
        }

        if (!includeHidden)
        {
            return sortBuilder
                .Descending(document => document.AdminReviewStatus)
                .Ascending(document => document.Name)
                .Ascending(document => document.Id);
        }

        return sortBuilder
            .Ascending(document => document.AdminReviewPriority)
            .Ascending(document => document.Name)
            .Ascending(document => document.Id);
    }
}
