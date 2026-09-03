using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserVisitMongoDefinitions
{
    public static FilterDefinition<UserVisitDocument> BuildOwnerFilter(string userId)
    {
        return Builders<UserVisitDocument>.Filter.Eq(
            static document => document.UserId,
            NormalizeRequired(userId, nameof(userId)));
    }

    public static FilterDefinition<UserVisitDocument> BuildOwnedVisitFilter(
        string visitId,
        string userId)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters = Builders<UserVisitDocument>.Filter;
        return filters.Eq(
                static document => document.Id,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)));
    }

    public static FilterDefinition<UserVisitDocument> BuildOwnedVersionFilter(
        string visitId,
        string userId,
        long expectedVersion)
    {
        if (expectedVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedVersion),
                "The expected visit version must be positive.");
        }

        return BuildOwnedVisitFilter(visitId, userId)
            & Builders<UserVisitDocument>.Filter.Eq(
                static document => document.Version,
                expectedVersion);
    }

    public static SortDefinition<UserVisitDocument> BuildNewestVisitSort()
    {
        return Builders<UserVisitDocument>.Sort
            .Descending(static document => document.Date.Year)
            .Descending(static document => document.Date.Month)
            .Descending(static document => document.Date.Day)
            .Descending(static document => document.UpdatedAt)
            .Ascending(static document => document.Id);
    }

    public static IReadOnlyCollection<CreateIndexModel<UserVisitDocument>> BuildIndexes()
    {
        return new List<CreateIndexModel<UserVisitDocument>>
        {
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.Date.Year)
                    .Descending(static document => document.Date.Month)
                    .Descending(static document => document.Date.Day),
                new CreateIndexOptions { Name = "idx_user_visits_user_date" }),
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ParkId)
                    .Descending(static document => document.Date.Year),
                new CreateIndexOptions { Name = "idx_user_visits_user_park_year" }),
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = "idx_user_visits_user_status_updated" }),
        };
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException("A non-empty identifier is required.", parameterName);
        }

        return normalizedValue;
    }
}
