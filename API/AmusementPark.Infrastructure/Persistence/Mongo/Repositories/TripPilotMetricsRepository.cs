using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripPilotMetricsRepository : ITripPilotMetricsRepository
{
    private readonly IMongoCollection<TripPlanDocument> plans;
    private readonly IMongoCollection<TripParkCandidateDocument> candidates;
    private readonly IMongoCollection<TripDayPlanDocument> days;
    private readonly IMongoCollection<TripInvitationDocument> invitations;
    private readonly IMongoCollection<TripItemPreferenceDocument> preferences;
    private readonly IMongoCollection<TripItemDecisionDocument> decisions;
    private readonly IMongoCollection<TripActivityEventDocument> activities;
    private readonly IMongoCollection<TripNotificationSubscriptionDocument> subscriptions;

    public TripPilotMetricsRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.plans = database.GetCollection<TripPlanDocument>(settings.TripPlansCollectionName);
        this.candidates = database.GetCollection<TripParkCandidateDocument>(
            settings.TripParkCandidatesCollectionName);
        this.days = database.GetCollection<TripDayPlanDocument>(
            settings.TripDayPlansCollectionName);
        this.invitations = database.GetCollection<TripInvitationDocument>(
            settings.TripInvitationsCollectionName);
        this.preferences = database.GetCollection<TripItemPreferenceDocument>(
            settings.TripItemPreferencesCollectionName);
        this.decisions = database.GetCollection<TripItemDecisionDocument>(
            settings.TripItemDecisionsCollectionName);
        this.activities = database.GetCollection<TripActivityEventDocument>(
            settings.TripAuditEventsCollectionName);
        this.subscriptions = database.GetCollection<TripNotificationSubscriptionDocument>(
            settings.TripNotificationSubscriptionsCollectionName);
    }

    public async Task<TripPilotMetricsSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<TripPlanDocument> activePlans = Builders<TripPlanDocument>.Filter.Eq(
            static plan => plan.DeletionState,
            TripDeletionState.None);
        long totalPlans = await this.plans.CountDocumentsAsync(
            activePlans,
            cancellationToken: cancellationToken);
        long collaborativePlans = await this.plans.CountDocumentsAsync(
            activePlans & BuildCollaborativePlanFilter(),
            cancellationToken: cancellationToken);
        long plansWithPreferences = await CountDistinctTripIdsAsync(
            this.preferences,
            Builders<TripItemPreferenceDocument>.Filter.Eq(
                static item => item.DocumentState,
                TripChildDocumentState.Committed),
            cancellationToken);
        long plansWithDecisions = await CountDistinctTripIdsAsync(
            this.decisions,
            Builders<TripItemDecisionDocument>.Filter.Eq(
                static item => item.DocumentState,
                TripChildDocumentState.Committed),
            cancellationToken);
        long expiredInvitations = await this.invitations.CountDocumentsAsync(
            Builders<TripInvitationDocument>.Filter.Eq(
                static item => item.Status,
                TripInvitationStatus.Expired),
            cancellationToken: cancellationToken);
        long enabledSubscriptions = await this.subscriptions.CountDocumentsAsync(
            Builders<TripNotificationSubscriptionDocument>.Filter.Eq(
                static item => item.IsEnabled,
                true),
            cancellationToken: cancellationToken);
        long auditEvents = await this.activities.CountDocumentsAsync(
            Builders<TripActivityEventDocument>.Filter.Empty,
            cancellationToken: cancellationToken);
        long pendingAuditMarkers =
            await CountPendingAuditMarkersAsync(this.plans, cancellationToken)
            + await CountPendingAuditMarkersAsync(this.candidates, cancellationToken)
            + await CountPendingAuditMarkersAsync(this.days, cancellationToken)
            + await CountPendingAuditMarkersAsync(this.invitations, cancellationToken)
            + await CountPendingAuditMarkersAsync(this.preferences, cancellationToken)
            + await CountPendingAuditMarkersAsync(this.decisions, cancellationToken);
        IReadOnlyDictionary<string, long> activityCounts = await CountActivitiesAsync(
            this.activities,
            cancellationToken);
        return new TripPilotMetricsSnapshot(
            totalPlans,
            collaborativePlans,
            plansWithPreferences,
            plansWithDecisions,
            expiredInvitations,
            enabledSubscriptions,
            auditEvents,
            pendingAuditMarkers,
            activityCounts);
    }

    internal static FilterDefinition<TripPlanDocument> BuildCollaborativePlanFilter()
    {
        BsonDocument activeMembers = new("$filter", new BsonDocument
        {
            { "input", "$members" },
            { "as", "member" },
            {
                "cond",
                new BsonDocument("$eq", new BsonArray
                {
                    "$$member.state",
                    TripMembershipState.Active.ToString(),
                })
            },
        });
        return new BsonDocumentFilterDefinition<TripPlanDocument>(
            new BsonDocument("$expr", new BsonDocument("$gte", new BsonArray
            {
                new BsonDocument("$size", activeMembers),
                2,
            })));
    }

    private static async Task<long> CountDistinctTripIdsAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken)
    {
        BsonDocument renderedFilter = filter.Render(new RenderArgs<TDocument>(
            collection.DocumentSerializer,
            collection.Settings.SerializerRegistry));
        BsonDocument? result = await collection.Aggregate<BsonDocument>(
                BuildDistinctTripCountPipeline(renderedFilter))
            .FirstOrDefaultAsync(cancellationToken);
        return result is null ? 0 : result["count"].ToInt64();
    }

    internal static BsonDocument[] BuildDistinctTripCountPipeline(BsonDocument matchFilter)
    {
        ArgumentNullException.ThrowIfNull(matchFilter);
        return new[]
        {
            new BsonDocument("$match", matchFilter),
            new BsonDocument("$group", new BsonDocument("_id", "$tripPlanId")),
            new BsonDocument("$count", "count"),
        };
    }

    private static async Task<long> CountPendingAuditMarkersAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        {
            new("$project", new BsonDocument(
                "count",
                new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray
                {
                    "$pendingAuditEvents",
                    new BsonArray(),
                })))),
            new("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "count", new BsonDocument("$sum", "$count") },
            }),
        };
        BsonDocument? result = await collection.Aggregate<BsonDocument>(pipeline)
            .FirstOrDefaultAsync(cancellationToken);
        return result is null ? 0 : result["count"].ToInt64();
    }

    private static async Task<IReadOnlyDictionary<string, long>> CountActivitiesAsync(
        IMongoCollection<TripActivityEventDocument> collection,
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        {
            new("$group", new BsonDocument
            {
                { "_id", "$kind" },
                { "count", new BsonDocument("$sum", 1) },
            }),
        };
        List<BsonDocument> rows = await collection.Aggregate<BsonDocument>(pipeline)
            .ToListAsync(cancellationToken);
        return rows
            .Where(static row => row.TryGetValue("_id", out BsonValue? key) && key.IsString)
            .ToDictionary(
                static row => row["_id"].AsString,
                static row => row["count"].ToInt64(),
                StringComparer.Ordinal);
    }
}
