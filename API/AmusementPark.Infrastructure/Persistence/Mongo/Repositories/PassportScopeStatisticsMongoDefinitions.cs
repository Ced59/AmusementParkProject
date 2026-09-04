using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class PassportScopeStatisticsMongoDefinitions
{
    public static FilterDefinition<UserVisitDocument> BuildParkVisitFilter(
        string userId,
        string parkId)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters =
            Builders<UserVisitDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                IdentifierRules.NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.ParkId,
                IdentifierRules.NormalizeRequired(parkId, nameof(parkId)))
            & filters.Ne(static document => document.Status, VisitStatus.Archived)
            & UserVisitMongoDefinitions.BuildNotDeletedFilter();
    }

    public static FilterDefinition<UserVisitDocument> BuildYearVisitFilter(
        string userId,
        int year)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters =
            Builders<UserVisitDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                IdentifierRules.NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(static document => document.Date.Year, year)
            & filters.Ne(static document => document.Status, VisitStatus.Archived)
            & UserVisitMongoDefinitions.BuildNotDeletedFilter();
    }

    public static FilterDefinition<UserRideOccurrenceDocument> BuildOccurrenceFilter(
        string userId,
        IReadOnlyCollection<string> visitIds)
    {
        ArgumentNullException.ThrowIfNull(visitIds);
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                IdentifierRules.NormalizeRequired(userId, nameof(userId)))
            & filters.In(static document => document.VisitId, visitIds)
            & filters.Eq(static document => document.DeletedAtUtc, null)
            & filters.Ne(static document => document.CreationPendingCompletion, true);
    }

    public static FilterDefinition<UserRatingDocument> BuildParkRatingFilter(
        string userId,
        string parkId)
    {
        FilterDefinitionBuilder<UserRatingDocument> filters =
            Builders<UserRatingDocument>.Filter;
        FilterDefinition<UserRatingDocument> supportedTargets =
            filters.Eq(static document => document.TargetType, RatingTargetType.Park)
            | (filters.Eq(static document => document.TargetType, RatingTargetType.ParkItem)
                & filters.Eq(
                    static document => document.ParkItemCategory,
                    ParkItemCategory.Attraction));
        return filters.Eq(
                static document => document.UserId,
                IdentifierRules.NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.ParkId,
                IdentifierRules.NormalizeRequired(parkId, nameof(parkId)))
            & filters.Ne(static document => document.IsMutationPlaceholder, true)
            & supportedTargets;
    }

    public static FilterDefinition<ParkItemDocument> BuildParkItemFilter(
        IReadOnlyCollection<string> parkItemIds)
    {
        return Builders<ParkItemDocument>.Filter.In(
            static document => document.Id,
            parkItemIds);
    }

    public static ProjectionDefinition<UserVisitDocument, PassportScopeVisitSourceDocument>
        BuildVisitProjection()
    {
        return Builders<UserVisitDocument>.Projection.Expression(
            static document => new PassportScopeVisitSourceDocument
            {
                Id = document.Id,
                ParkId = document.ParkId,
                Date = document.Date,
                ParkAssessmentValueHalfSteps = document.ParkAssessment == null
                    ? null
                    : document.ParkAssessment.ValueHalfSteps,
                ContentMutationFenceToken = document.ContentMutationFenceToken,
                ContentMutationFenceStableToken = document.ContentMutationFenceStableToken,
                ContentMutationFenceReady = document.ContentMutationFenceReady,
            });
    }

    public static ProjectionDefinition<
        UserRideOccurrenceDocument,
        PassportScopeOccurrenceSourceDocument> BuildOccurrenceProjection()
    {
        return Builders<UserRideOccurrenceDocument>.Projection.Expression(
            static document => new PassportScopeOccurrenceSourceDocument
            {
                Id = document.Id,
                VisitId = document.VisitId,
                ParkId = document.ParkId,
                ParkItemId = document.ParkItemId,
                Status = document.Status,
                AssessmentValueHalfSteps = document.Assessment == null
                    ? null
                    : document.Assessment.ValueHalfSteps,
                HistoricalCategory = document.HistoricalTarget == null
                    ? null
                    : document.HistoricalTarget.Category,
                ContentMutationFenceToken = document.ContentMutationFenceToken,
            });
    }

    public static ProjectionDefinition<ParkItemDocument, PassportScopeParkItemSourceDocument>
        BuildParkItemProjection()
    {
        return Builders<ParkItemDocument>.Projection.Expression(
            static document => new PassportScopeParkItemSourceDocument
            {
                Id = document.Id,
                Category = document.Category,
            });
    }

    public static ProjectionDefinition<UserRatingDocument, PassportScopeRatingSourceDocument>
        BuildRatingProjection()
    {
        return Builders<UserRatingDocument>.Projection.Expression(
            static document => new PassportScopeRatingSourceDocument
            {
                TargetType = document.TargetType,
                TargetId = document.TargetId,
                Value = document.Value,
            });
    }
}

internal sealed class PassportScopeVisitSourceDocument
{
    public string Id { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public VisitDateDocument Date { get; init; } = new VisitDateDocument();
    public byte? ParkAssessmentValueHalfSteps { get; init; }
    public long? ContentMutationFenceToken { get; init; }
    public long? ContentMutationFenceStableToken { get; init; }
    public bool ContentMutationFenceReady { get; init; }
}

internal sealed class PassportScopeOccurrenceSourceDocument
{
    public string Id { get; init; } = string.Empty;
    public string VisitId { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public string ParkItemId { get; init; } = string.Empty;
    public RideOccurrenceStatus Status { get; init; }
    public byte? AssessmentValueHalfSteps { get; init; }
    public string? HistoricalCategory { get; init; }
    public long? ContentMutationFenceToken { get; init; }
}

internal sealed class PassportScopeParkItemSourceDocument
{
    public string Id { get; init; } = string.Empty;
    public ParkItemCategory Category { get; init; }
}

internal sealed class PassportScopeRatingSourceDocument
{
    public RatingTargetType TargetType { get; init; }
    public string TargetId { get; init; } = string.Empty;
    public double Value { get; init; }
}
