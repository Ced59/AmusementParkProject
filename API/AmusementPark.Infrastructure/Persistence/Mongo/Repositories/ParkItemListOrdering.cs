using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkItemListOrdering
{
    public static SortDefinition<ParkItemDocument> Build(ParkItemAdminSortField sortField, bool sortDescending)
    {
        SortDefinitionBuilder<ParkItemDocument> sortBuilder = Builders<ParkItemDocument>.Sort;

        if (sortField == ParkItemAdminSortField.Default)
        {
            return sortBuilder
                .Ascending(document => document.AdminReviewPriority)
                .Ascending(document => document.ParkId)
                .Ascending(document => document.Name)
                .Ascending(document => document.Id);
        }

        SortDefinition<ParkItemDocument> primarySort = BuildPrimary(sortField, sortDescending, sortBuilder);
        return sortBuilder.Combine(primarySort, sortBuilder.Ascending(document => document.Name), sortBuilder.Ascending(document => document.Id));
    }

    private static SortDefinition<ParkItemDocument> BuildPrimary(ParkItemAdminSortField sortField, bool sortDescending, SortDefinitionBuilder<ParkItemDocument> sortBuilder)
    {
        switch (sortField)
        {
            case ParkItemAdminSortField.Name:
                return sortDescending ? sortBuilder.Descending(document => document.Name) : sortBuilder.Ascending(document => document.Name);
            case ParkItemAdminSortField.Category:
                return sortDescending ? sortBuilder.Descending(document => document.Category) : sortBuilder.Ascending(document => document.Category);
            case ParkItemAdminSortField.Type:
                return sortDescending ? sortBuilder.Descending(document => document.Type) : sortBuilder.Ascending(document => document.Type);
            case ParkItemAdminSortField.IsVisible:
                return sortDescending ? sortBuilder.Descending(document => document.IsVisible) : sortBuilder.Ascending(document => document.IsVisible);
            case ParkItemAdminSortField.AdminReviewStatus:
                return sortDescending ? sortBuilder.Descending(document => document.AdminReviewPriority) : sortBuilder.Ascending(document => document.AdminReviewPriority);
            case ParkItemAdminSortField.ParkId:
                return sortDescending ? sortBuilder.Descending(document => document.ParkId) : sortBuilder.Ascending(document => document.ParkId);
            case ParkItemAdminSortField.ZoneId:
                return sortDescending ? sortBuilder.Descending(document => document.ZoneId) : sortBuilder.Ascending(document => document.ZoneId);
            default:
                return sortBuilder.Ascending(document => document.AdminReviewPriority);
        }
    }
}
