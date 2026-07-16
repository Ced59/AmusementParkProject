using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class StandaloneAttractionListOrdering
{
    public static SortDefinition<StandaloneAttractionDocument> Build(StandaloneAttractionAdminSortField sortField, bool sortDescending)
    {
        SortDefinitionBuilder<StandaloneAttractionDocument> sortBuilder = Builders<StandaloneAttractionDocument>.Sort;
        SortDefinition<StandaloneAttractionDocument> primarySort = sortField switch
        {
            StandaloneAttractionAdminSortField.Name => sortDescending ? sortBuilder.Descending(document => document.Name) : sortBuilder.Ascending(document => document.Name),
            StandaloneAttractionAdminSortField.Type => sortDescending ? sortBuilder.Descending(document => document.Type) : sortBuilder.Ascending(document => document.Type),
            StandaloneAttractionAdminSortField.CountryCode => sortDescending ? sortBuilder.Descending(document => document.CountryCode) : sortBuilder.Ascending(document => document.CountryCode),
            StandaloneAttractionAdminSortField.IsVisible => sortDescending ? sortBuilder.Descending(document => document.IsVisible) : sortBuilder.Ascending(document => document.IsVisible),
            StandaloneAttractionAdminSortField.AdminReviewStatus => sortDescending ? sortBuilder.Descending(document => document.AdminReviewPriority) : sortBuilder.Ascending(document => document.AdminReviewPriority),
            StandaloneAttractionAdminSortField.CreatedAt => sortDescending ? sortBuilder.Descending(document => document.CreatedAt) : sortBuilder.Ascending(document => document.CreatedAt),
            StandaloneAttractionAdminSortField.UpdatedAt => sortDescending ? sortBuilder.Descending(document => document.UpdatedAt) : sortBuilder.Ascending(document => document.UpdatedAt),
            _ => sortBuilder.Ascending(document => document.AdminReviewPriority),
        };

        return sortBuilder.Combine(
            primarySort,
            sortBuilder.Ascending(document => document.Name),
            sortBuilder.Ascending(document => document.Id));
    }
}
