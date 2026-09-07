using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ShareSourceRevisionMongoDefinitions
{
    public static FilterDefinition<ShareSourceRevisionDocument> BuildScopeFilter(string scopeKey)
    {
        return Builders<ShareSourceRevisionDocument>.Filter.Eq(
            document => document.ScopeKey,
            scopeKey);
    }

    public static FilterDefinition<ShareSourceRevisionDocument> BuildActiveLeaseFilter(
        string scopeKey,
        string token)
    {
        return BuildLeaseExpirationFilter(scopeKey, token, "$gt");
    }

    public static FilterDefinition<ShareSourceRevisionDocument> BuildExpiredLeaseFilter(
        string scopeKey,
        string token)
    {
        return Builders<ShareSourceRevisionDocument>.Filter.Lt(
                document => document.Revision,
                long.MaxValue)
            & BuildLeaseExpirationFilter(scopeKey, token, "$lte");
    }

    public static FilterDefinition<ShareSourceRevisionDocument> BuildExpiredLeaseFilter(
        string scopeKey,
        DateTime nowUtc)
    {
        return BuildScopeFilter(scopeKey)
            & Builders<ShareSourceRevisionDocument>.Filter.Lt(
                document => document.Revision,
                long.MaxValue)
            & Builders<ShareSourceRevisionDocument>.Filter.ElemMatch(
                document => document.MutationLeases,
                lease => lease.ExpiresAtUtc <= nowUtc);
    }

    private static FilterDefinition<ShareSourceRevisionDocument> BuildLeaseExpirationFilter(
        string scopeKey,
        string token,
        string comparisonOperator)
    {
        BsonDocument matchingLease = new BsonDocument(
            "$and",
            new BsonArray
            {
                new BsonDocument(
                    "$eq",
                    new BsonArray { "$$lease.token", token }),
                new BsonDocument(
                    comparisonOperator,
                    new BsonArray { "$$lease.expiresAtUtc", "$$NOW" }),
            });
        BsonDocument expression = new BsonDocument(
            "$expr",
            new BsonDocument(
                "$anyElementTrue",
                new BsonArray
                {
                    new BsonDocument(
                        "$map",
                        new BsonDocument
                        {
                            {
                                "input",
                                new BsonDocument(
                                    "$ifNull",
                                    new BsonArray { "$mutationLeases", new BsonArray() })
                            },
                            { "as", "lease" },
                            { "in", matchingLease },
                        }),
                }));
        return BuildScopeFilter(scopeKey)
            & new BsonDocumentFilterDefinition<ShareSourceRevisionDocument>(expression);
    }
}
