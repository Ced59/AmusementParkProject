using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
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

    public static FilterDefinition<ShareSourceRevisionDocument> BuildLeaseFilter(
        string scopeKey,
        string token)
    {
        return BuildScopeFilter(scopeKey)
            & Builders<ShareSourceRevisionDocument>.Filter.ElemMatch(
                document => document.MutationLeases,
                lease => lease.Token == token);
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
}
