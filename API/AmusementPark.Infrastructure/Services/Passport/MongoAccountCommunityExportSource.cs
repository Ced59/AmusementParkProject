using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialShare;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.Passport;

public sealed class MongoAccountCommunityExportSource : IAccountCommunityExportSource
{
    private const int MaximumItemsPerCollection = 10_000;
    private readonly IMongoCollection<CommentDocument> comments;
    private readonly IMongoCollection<ImageDocument> images;
    private readonly IMongoCollection<HistoricalExistenceReportDocument> historicalReports;
    private readonly IMongoCollection<SocialShareEventDocument> socialShares;
    private readonly IMongoCollection<ParkDocument> parks;
    private readonly IMongoCollection<ParkItemDocument> parkItems;

    public MongoAccountCommunityExportSource(IMongoDatabase database, MongoDbSettings settings)
    {
        this.comments = database.GetCollection<CommentDocument>(settings.CommentsCollectionName);
        this.images = database.GetCollection<ImageDocument>(settings.ImagesCollectionName);
        this.historicalReports = database.GetCollection<HistoricalExistenceReportDocument>(
            settings.HistoricalExistenceReportsCollectionName);
        this.socialShares = database.GetCollection<SocialShareEventDocument>(
            settings.SocialShareEventsCollectionName);
        this.parks = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItems = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
    }

    public async Task<AccountCommunityExportData> LoadAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        List<CommentDocument> commentDocuments = await this.comments
            .Find(document => document.AuthorUserId == userId)
            .SortBy(static document => document.CreatedAt)
            .Limit(MaximumItemsPerCollection + 1)
            .ToListAsync(cancellationToken);
        List<HistoricalExistenceReportDocument> reportDocuments = await this.historicalReports
            .Find(document => document.OwnerUserId == userId)
            .SortBy(static document => document.SubmittedAtUtc)
            .Limit(MaximumItemsPerCollection + 1)
            .ToListAsync(cancellationToken);
        List<SocialShareEventDocument> socialShareDocuments = await this.socialShares
            .Find(document => document.UserId == userId)
            .SortBy(static document => document.OccurredAtUtc)
            .Limit(MaximumItemsPerCollection + 1)
            .ToListAsync(cancellationToken);
        ThrowIfCollectionLimitExceeded(commentDocuments, reportDocuments, socialShareDocuments);

        string[] commentIds = commentDocuments.Select(static document => document.Id).ToArray();
        string[] commentImageIds = commentDocuments
            .SelectMany(static document => document.ImageIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        FilterDefinitionBuilder<ImageDocument> imageFilters = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> imageFilter =
            imageFilters.Eq(static document => document.DraftOwnerId, userId)
            | (imageFilters.Eq(static document => document.OwnerType, ImageOwnerType.User)
                & imageFilters.Eq(static document => document.OwnerId, userId));
        if (commentImageIds.Length > 0)
        {
            imageFilter |= imageFilters.In(static document => document.Id, commentImageIds);
        }

        if (commentIds.Length > 0)
        {
            imageFilter |= imageFilters.Eq(static document => document.OwnerType, ImageOwnerType.Comment)
                & imageFilters.In(static document => document.OwnerId, commentIds);
        }

        List<ImageDocument> imageDocuments = await this.images.Find(imageFilter)
            .SortBy(static document => document.CreatedAt)
            .Limit(MaximumItemsPerCollection + 1)
            .ToListAsync(cancellationToken);
        if (imageDocuments.Count > MaximumItemsPerCollection)
        {
            throw new PassportExportSizeLimitException();
        }

        IReadOnlyDictionary<string, ParkDocument> parksById = await this.LoadParksAsync(
            commentDocuments,
            cancellationToken);
        IReadOnlyDictionary<string, ParkItemDocument> parkItemsById = await this.LoadParkItemsAsync(
            commentDocuments,
            cancellationToken);
        Dictionary<string, string> imageReferences = imageDocuments
            .Select((document, index) => (document.Id, Reference: $"image-{index + 1:D4}"))
            .ToDictionary(static value => value.Id, static value => value.Reference, StringComparer.Ordinal);

        return new AccountCommunityExportData(
            commentDocuments.Select((document, index) => MapComment(
                document,
                $"comment-{index + 1:D4}",
                imageReferences,
                parksById,
                parkItemsById)).ToArray(),
            imageDocuments.Select((document, index) => MapImage(
                document,
                $"image-{index + 1:D4}")).ToArray(),
            reportDocuments.Select(MapHistoricalReport).ToArray(),
            socialShareDocuments.Select(MapSocialShare).ToArray());
    }

    private async Task<IReadOnlyDictionary<string, ParkDocument>> LoadParksAsync(
        IReadOnlyCollection<CommentDocument> commentDocuments,
        CancellationToken cancellationToken)
    {
        string[] parkIds = commentDocuments.Select(static document => document.ParkId)
            .Concat(commentDocuments
                .Where(static document => document.TargetType == CommentTargetType.Park)
                .Select(static document => document.TargetId))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (parkIds.Length == 0)
        {
            return new Dictionary<string, ParkDocument>(StringComparer.Ordinal);
        }

        List<ParkDocument> documents = await this.parks
            .Find(Builders<ParkDocument>.Filter.In(static document => document.Id, parkIds))
            .ToListAsync(cancellationToken);
        return documents.ToDictionary(static document => document.Id, StringComparer.Ordinal);
    }

    private async Task<IReadOnlyDictionary<string, ParkItemDocument>> LoadParkItemsAsync(
        IReadOnlyCollection<CommentDocument> commentDocuments,
        CancellationToken cancellationToken)
    {
        string[] parkItemIds = commentDocuments
            .Where(static document => document.TargetType == CommentTargetType.ParkItem)
            .Select(static document => document.TargetId)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (parkItemIds.Length == 0)
        {
            return new Dictionary<string, ParkItemDocument>(StringComparer.Ordinal);
        }

        List<ParkItemDocument> documents = await this.parkItems
            .Find(Builders<ParkItemDocument>.Filter.In(static document => document.Id, parkItemIds))
            .ToListAsync(cancellationToken);
        return documents.ToDictionary(static document => document.Id, StringComparer.Ordinal);
    }

    internal static AccountCommentExportData MapComment(
        CommentDocument document,
        string reference,
        IReadOnlyDictionary<string, string> imageReferences,
        IReadOnlyDictionary<string, ParkDocument> parksById,
        IReadOnlyDictionary<string, ParkItemDocument> parkItemsById)
    {
        parksById.TryGetValue(document.ParkId, out ParkDocument? park);
        string? targetName = document.TargetType switch
        {
            CommentTargetType.Park when parksById.TryGetValue(document.TargetId, out ParkDocument? targetPark)
                => targetPark.Name,
            CommentTargetType.ParkItem when parkItemsById.TryGetValue(
                document.TargetId,
                out ParkItemDocument? targetItem) => targetItem.Name,
            _ => null,
        };
        return new AccountCommentExportData(
            reference,
            document.TargetType.ToString(),
            targetName,
            park?.Name,
            document.Bodies.Select(static value => new AccountLocalizedTextExportData(
                value.LanguageCode,
                value.Value)).ToArray(),
            document.ImageIds.Where(imageReferences.ContainsKey)
                .Select(imageId => imageReferences[imageId])
                .ToArray(),
            document.IsOfficial,
            document.ModerationStatus.ToString(),
            document.Revision,
            document.CreatedAt,
            document.UpdatedAt);
    }

    internal static AccountImageExportData MapImage(ImageDocument document, string reference)
    {
        return new AccountImageExportData(
            reference,
            document.OriginalFileName,
            document.ContentType,
            document.Description,
            document.AltTexts.Select(static value => new AccountLocalizedTextExportData(
                value.LanguageCode,
                value.Value)).ToArray(),
            document.Captions.Select(static value => new AccountLocalizedTextExportData(
                value.LanguageCode,
                value.Value)).ToArray(),
            document.Credits.Select(static value => new AccountLocalizedTextExportData(
                value.LanguageCode,
                value.Value)).ToArray(),
            document.Width,
            document.Height,
            document.SizeInBytes,
            document.IsPublished,
            document.SourceUrl,
            document.GeoLocation?.Latitude,
            document.GeoLocation?.Longitude,
            document.ExifMetadata?.CameraMaker,
            document.ExifMetadata?.CameraModel,
            document.ExifMetadata?.TakenOnUtc,
            document.CreatedAt,
            document.UpdatedAt);
    }

    internal static AccountHistoricalReportExportData MapHistoricalReport(
        HistoricalExistenceReportDocument document)
    {
        return new AccountHistoricalReportExportData(
            document.ParkName,
            document.VisitDate.Year,
            document.VisitDate.Month,
            document.VisitDate.Day,
            document.VisitDate.Precision.ToString(),
            document.VisitDate.IsApproximate,
            document.ClaimedName,
            document.SourceUrl,
            document.SourceReference,
            document.Details,
            document.Status.ToString(),
            document.SubmittedAtUtc,
            document.ReviewedAtUtc,
            document.DecisionNote,
            document.Revision);
    }

    internal static AccountSocialShareExportData MapSocialShare(
        SocialShareEventDocument document)
    {
        return new AccountSocialShareExportData(
            document.OccurredAtUtc,
            document.TargetType,
            document.TargetTitle,
            document.LanguageCode,
            document.Channel);
    }

    private static void ThrowIfCollectionLimitExceeded(
        IReadOnlyCollection<CommentDocument> comments,
        IReadOnlyCollection<HistoricalExistenceReportDocument> historicalReports,
        IReadOnlyCollection<SocialShareEventDocument> socialShares)
    {
        if (comments.Count > MaximumItemsPerCollection
            || historicalReports.Count > MaximumItemsPerCollection
            || socialShares.Count > MaximumItemsPerCollection)
        {
            throw new PassportExportSizeLimitException();
        }
    }
}
