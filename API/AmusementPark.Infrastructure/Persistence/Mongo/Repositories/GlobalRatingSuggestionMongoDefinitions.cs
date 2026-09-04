using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class GlobalRatingSuggestionMongoDefinitions
{
    public static IReadOnlyCollection<CreateIndexModel<GlobalRatingSuggestionStateDocument>>
        BuildStateIndexes()
    {
        return new[]
        {
            new CreateIndexModel<GlobalRatingSuggestionStateDocument>(
                Builders<GlobalRatingSuggestionStateDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId),
                new CreateIndexOptions
                {
                    Name = "ratingSuggestionState_user_target_unique",
                    Unique = true,
                }),
        };
    }

    public static IReadOnlyCollection<CreateIndexModel<GlobalRatingSuggestionPreferenceDocument>>
        BuildPreferenceIndexes()
    {
        return new[]
        {
            new CreateIndexModel<GlobalRatingSuggestionPreferenceDocument>(
                Builders<GlobalRatingSuggestionPreferenceDocument>.IndexKeys
                    .Ascending(static document => document.UserId),
                new CreateIndexOptions
                {
                    Name = "ratingSuggestionPreference_user_unique",
                    Unique = true,
                }),
        };
    }

    public static IReadOnlyCollection<CreateIndexModel<GlobalRatingSuggestionInteractionDocument>>
        BuildInteractionIndexes()
    {
        return new[]
        {
            new CreateIndexModel<GlobalRatingSuggestionInteractionDocument>(
                Builders<GlobalRatingSuggestionInteractionDocument>.IndexKeys
                    .Ascending(static document => document.OccurredAtUtc),
                new CreateIndexOptions
                {
                    Name = "ratingSuggestionInteraction_ttl",
                    ExpireAfter = TimeSpan.FromDays(400),
                }),
            new CreateIndexModel<GlobalRatingSuggestionInteractionDocument>(
                Builders<GlobalRatingSuggestionInteractionDocument>.IndexKeys
                    .Ascending(static document => document.InteractionType)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions
                {
                    Name = "ratingSuggestionInteraction_type_date",
                }),
        };
    }
}
