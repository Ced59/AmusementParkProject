using AmusementPark.Application.Features.CaptainCoaster.Results;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Core.Domain.Contact;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Contact;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using System.Globalization;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialPublishing;
using AmusementPark.Core.Domain.SocialShare;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialShare;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;
using MongoDB.Bson;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Weather;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

/// <summary>
/// Mappers centralisés domaine/resultats applicatifs &lt;-&gt; documents Mongo.
/// </summary>
internal static class EntityMongoMappers
{
public static CaptainCoasterSettingsResult ToResult(this CaptainCoasterSettingsDocument document)
    {
        return new CaptainCoasterSettingsResult
        {
            IsEnabled = document.IsEnabled,
            DataDirectoryPath = document.DataDirectoryPath,
            HtmlDirectoryPath = document.HtmlDirectoryPath,
            UseOfflineMode = document.UseOfflineMode,
        };
    }

    public static CaptainCoasterSettingsDocument ToDocument(this CaptainCoasterSettingsResult result)
    {
        return new CaptainCoasterSettingsDocument
        {
            IsEnabled = result.IsEnabled,
            DataDirectoryPath = result.DataDirectoryPath,
            HtmlDirectoryPath = result.HtmlDirectoryPath,
            UseOfflineMode = result.UseOfflineMode,
        };
    }

    public static CaptainCoasterSessionResult ToResult(this CaptainCoasterSyncSessionDocument document)
    {
        return new CaptainCoasterSessionResult
        {
            SessionId = document.Id,
            Status = document.Status,
            ProgressPercentage = document.ProgressPercentage,
            Message = document.Message,
        };
    }

    public static AmusementPark.Application.Features.Search.Results.SearchHitResult ToSearchHit(this SearchItemDocument document, string? languageCode = null)
    {
        string? localizedDescription = SearchLocalizedTextResolver.Resolve(document.LocalizedDescriptions, languageCode);

        return new AmusementPark.Application.Features.Search.Results.SearchHitResult
        {
            Id = string.IsNullOrWhiteSpace(document.OriginalId) ? document.Id : document.OriginalId,
            ResourceType = string.IsNullOrWhiteSpace(document.ResourceType) ? document.Category : document.ResourceType,
            Title = document.Title,
            Subtitle = document.Subtitle,
            Category = document.Category,
            Description = localizedDescription ?? document.Description,
            City = document.City,
            CountryCode = document.CountryCode,
            LogoImageId = document.LogoImageId,
            AttractionCount = document.AttractionCount,
            ParkStatus = document.ParkStatus,
            ParentParkId = document.ParentParkId,
            ParentParkName = document.ParentParkName,
            Score = document.CompositeScore,
        };
    }

public static Comment ToDomain(this CommentDocument document)
    {
        return new Comment
        {
            Id = document.Id,
            TargetType = document.TargetType,
            TargetId = document.TargetId,
            ParkId = document.ParkId,
            AuthorUserId = document.AuthorUserId,
            AuthorDisplayName = string.IsNullOrWhiteSpace(document.AuthorDisplayName)
                ? "User"
                : document.AuthorDisplayName,
            AuthorAvatarUrl = document.AuthorAvatarUrl,
            AuthorRole = document.AuthorRole,
            Bodies = CommonMongoMappers.ToDomain(document.Bodies),
            ImageIds = document.ImageIds,
            Revision = document.Revision,
            IsOfficial = document.IsOfficial,
            ModerationStatus = document.ModerationStatus,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
        };
    }

    public static CommentDocument ToDocument(this Comment entity)
    {
        return new CommentDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            ParkId = entity.ParkId,
            AuthorUserId = entity.AuthorUserId,
            AuthorDisplayName = entity.AuthorDisplayName,
            AuthorAvatarUrl = entity.AuthorAvatarUrl,
            AuthorRole = entity.AuthorRole,
            Bodies = CommonMongoMappers.ToDocuments(entity.Bodies),
            ImageIds = entity.ImageIds,
            Revision = entity.Revision,
            IsOfficial = entity.IsOfficial,
            ModerationStatus = entity.ModerationStatus,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

public static ContactGrievance ToDomain(this ContactGrievanceDocument document)
    {
        ContactGrievance entity = new ContactGrievance
        {
            Id = document.Id,
            Message = document.Message,
            LanguageCode = document.LanguageCode,
            IpAddress = document.IpAddress,
            UserAgent = document.UserAgent,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ContactGrievanceDocument ToDocument(this ContactGrievance entity)
    {
        return new ContactGrievanceDocument
        {
            Id = entity.Id,
            Message = entity.Message,
            LanguageCode = entity.LanguageCode,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

public static HistoryEvent ToDomain(this HistoryEventDocument document)
    {
        HistoryEvent entity = new HistoryEvent
        {
            Id = document.Id,
            Key = document.Key,
            EntityType = document.EntityType,
            OwnerId = document.OwnerId,
            ParkId = document.ParkId,
            ParkItemId = document.ParkItemId,
            ContextParkId = document.ContextParkId,
            Year = document.Year,
            Month = document.Month,
            Day = document.Day,
            DatePrecision = document.DatePrecision,
            EventType = document.EventType,
            IsMajor = document.IsMajor,
            IsVisible = document.IsVisible,
            Slug = document.Slug,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Summaries = CommonMongoMappers.ToDomain(document.Summaries),
            MainImageId = document.MainImageId,
            PreviousName = document.PreviousName,
            NewName = document.NewName,
            PreviousLogoImageId = document.PreviousLogoImageId,
            NewLogoImageId = document.NewLogoImageId,
            PreviousOperatorId = document.PreviousOperatorId,
            NewOperatorId = document.NewOperatorId,
            LocationLabel = document.LocationLabel,
            RelatedParkIds = document.RelatedParkIds.ToList(),
            RelatedParkItemIds = document.RelatedParkItemIds.ToList(),
            Sources = document.Sources.Select(ToDomain).ToList(),
            Article = document.Article?.ToDomain(),
            CanonicalFactId = Guid.TryParse(document.CanonicalFactId, out Guid canonicalFactId)
                ? canonicalFactId
                : null,
            CanonicalizationState = document.CanonicalizationState,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static HistoryEventDocument ToDocument(this HistoryEvent entity)
    {
        return new HistoryEventDocument
        {
            Id = entity.Id,
            Key = entity.Key,
            EntityType = entity.EntityType,
            OwnerId = entity.OwnerId,
            ParkId = entity.ParkId,
            ParkItemId = entity.ParkItemId,
            ContextParkId = entity.ContextParkId,
            Year = entity.Year,
            Month = entity.Month,
            Day = entity.Day,
            DatePrecision = entity.DatePrecision,
            EventType = entity.EventType,
            IsMajor = entity.IsMajor,
            IsVisible = entity.IsVisible,
            Slug = entity.Slug,
            Titles = CommonMongoMappers.ToDocuments(entity.Titles),
            Summaries = CommonMongoMappers.ToDocuments(entity.Summaries),
            MainImageId = entity.MainImageId,
            PreviousName = entity.PreviousName,
            NewName = entity.NewName,
            PreviousLogoImageId = entity.PreviousLogoImageId,
            NewLogoImageId = entity.NewLogoImageId,
            PreviousOperatorId = entity.PreviousOperatorId,
            NewOperatorId = entity.NewOperatorId,
            LocationLabel = entity.LocationLabel,
            RelatedParkIds = entity.RelatedParkIds.ToList(),
            RelatedParkItemIds = entity.RelatedParkItemIds.ToList(),
            Sources = entity.Sources.Select(ToDocument).ToList(),
            Article = entity.Article?.ToDocument(),
            CanonicalFactId = entity.CanonicalFactId?.ToString("N"),
            CanonicalizationState = entity.CanonicalizationState,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    private static HistorySourceReference ToDomain(this HistorySourceReferenceDocument document)
    {
        return new HistorySourceReference
        {
            Label = document.Label,
            Url = document.Url,
            AccessedAt = document.AccessedAt,
        };
    }

    private static HistorySourceReferenceDocument ToDocument(this HistorySourceReference entity)
    {
        return new HistorySourceReferenceDocument
        {
            Label = entity.Label,
            Url = entity.Url,
            AccessedAt = entity.AccessedAt,
        };
    }

    private static HistoryArticle ToDomain(this HistoryArticleDocument document)
    {
        return new HistoryArticle
        {
            Slug = document.Slug,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Subtitles = CommonMongoMappers.ToDomain(document.Subtitles),
            Summaries = CommonMongoMappers.ToDomain(document.Summaries),
            MainImageId = document.MainImageId,
            Blocks = document.Blocks.Select(ToDomain).ToList(),
            Sources = document.Sources.Select(ToDomain).ToList(),
            IsPublished = document.IsPublished,
        };
    }

    private static HistoryArticleDocument ToDocument(this HistoryArticle entity)
    {
        return new HistoryArticleDocument
        {
            Slug = entity.Slug,
            Titles = CommonMongoMappers.ToDocuments(entity.Titles),
            Subtitles = CommonMongoMappers.ToDocuments(entity.Subtitles),
            Summaries = CommonMongoMappers.ToDocuments(entity.Summaries),
            MainImageId = entity.MainImageId,
            Blocks = entity.Blocks.Select(ToDocument).ToList(),
            Sources = entity.Sources.Select(ToDocument).ToList(),
            IsPublished = entity.IsPublished,
        };
    }

    private static HistoryArticleBlock ToDomain(this HistoryArticleBlockDocument document)
    {
        return new HistoryArticleBlock
        {
            Id = document.Id,
            Type = document.Type,
            SortOrder = document.SortOrder,
            HeadingLevel = document.HeadingLevel,
            Texts = CommonMongoMappers.ToDomain(document.Texts),
            ImageId = document.ImageId,
            ImageIds = document.ImageIds.ToList(),
            Captions = CommonMongoMappers.ToDomain(document.Captions),
        };
    }

    private static HistoryArticleBlockDocument ToDocument(this HistoryArticleBlock entity)
    {
        return new HistoryArticleBlockDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            Type = entity.Type,
            SortOrder = entity.SortOrder,
            HeadingLevel = entity.HeadingLevel,
            Texts = CommonMongoMappers.ToDocuments(entity.Texts),
            ImageId = entity.ImageId,
            ImageIds = entity.ImageIds.ToList(),
            Captions = CommonMongoMappers.ToDocuments(entity.Captions),
        };
    }

public static Image ToDomain(this ImageDocument document)
    {
        bool usesLegacyReservationDeadline =
            document.OwnerType == ImageOwnerType.CommentDraft
            && !string.IsNullOrWhiteSpace(document.PendingCommentId)
            && document.ReservationReconcileAfter is null
            && document.CleanupRequestedAt.HasValue;
        Image entity = new Image
        {
            Id = document.Id,
            Category = document.Category,
            Path = document.Path,
            Description = document.Description,
            AltTexts = CommonMongoMappers.ToDomain(document.AltTexts),
            Captions = CommonMongoMappers.ToDomain(document.Captions),
            Credits = CommonMongoMappers.ToDomain(document.Credits),
            TagIds = document.TagIds,
            GeoLocation = CommonMongoMappers.ToDomain(document.GeoLocation),
            ExifMetadata = document.ExifMetadata?.ToDomain(),
            Width = document.Width,
            Height = document.Height,
            SizeInBytes = document.SizeInBytes,
            OwnerType = document.OwnerType,
            OwnerId = document.OwnerId,
            IsCurrent = document.IsCurrent,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            SourceUrl = document.SourceUrl,
            IsWatermarked = document.IsWatermarked,
            IsPublished = document.IsPublished,
            DraftOwnerId = document.DraftOwnerId,
            PendingCommentId = document.PendingCommentId,
            PendingReservationToken = document.PendingReservationToken,
            PendingCommentRevision = document.PendingCommentRevision,
            PendingReservationExpiresAtUtc =
                document.PendingReservationExpiresAt,
            AbortedReservationTokens =
                document.AbortedReservationTokens.ToList(),
            CleanupRequestedAtUtc = usesLegacyReservationDeadline
                ? null
                : document.CleanupRequestedAt,
            CleanupCommentRevision = usesLegacyReservationDeadline
                ? null
                : document.CleanupCommentRevision,
            ReservationReconcileAfterUtc =
                document.ReservationReconcileAfter
                ?? (usesLegacyReservationDeadline
                    ? document.CleanupRequestedAt
                    : null),
            CommentReuseReservationToken = document.CommentReuseReservationToken,
            CommentReuseReconcileAfterUtc = document.CommentReuseReconcileAfter,
            CommentReuseTargetRevision = document.CommentReuseTargetRevision,
            CommentReuseExpiresAtUtc = document.CommentReuseExpiresAt,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ImageDocument ToDocument(this Image entity)
    {
        return new ImageDocument
        {
            Id = entity.Id,
            Category = entity.Category,
            Path = entity.Path,
            Description = entity.Description,
            AltTexts = CommonMongoMappers.ToDocuments(entity.AltTexts),
            Captions = CommonMongoMappers.ToDocuments(entity.Captions),
            Credits = CommonMongoMappers.ToDocuments(entity.Credits),
            TagIds = entity.TagIds,
            GeoLocation = CommonMongoMappers.ToDocument(entity.GeoLocation),
            ExifMetadata = entity.ExifMetadata?.ToDocument(),
            Width = entity.Width,
            Height = entity.Height,
            SizeInBytes = entity.SizeInBytes,
            OwnerType = entity.OwnerType,
            OwnerId = entity.OwnerId,
            IsCurrent = entity.IsCurrent,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            SourceUrl = entity.SourceUrl,
            IsWatermarked = entity.IsWatermarked,
            IsPublished = entity.IsPublished,
            DraftOwnerId = entity.DraftOwnerId,
            PendingCommentId = entity.PendingCommentId,
            PendingReservationToken = entity.PendingReservationToken,
            PendingCommentRevision = entity.PendingCommentRevision,
            PendingReservationExpiresAt =
                entity.PendingReservationExpiresAtUtc,
            AbortedReservationTokens =
                entity.AbortedReservationTokens.ToList(),
            ReservationReconcileAfter =
                entity.ReservationReconcileAfterUtc,
            CleanupRequestedAt = entity.CleanupRequestedAtUtc,
            CleanupCommentRevision = entity.CleanupCommentRevision,
            CommentReuseReservationToken = entity.CommentReuseReservationToken,
            CommentReuseReconcileAfter = entity.CommentReuseReconcileAfterUtc,
            CommentReuseTargetRevision = entity.CommentReuseTargetRevision,
            CommentReuseExpiresAt = entity.CommentReuseExpiresAtUtc,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ImageExifMetadata ToDomain(this ImageExifMetadataDocument document)
    {
        return new ImageExifMetadata
        {
            CameraMaker = document.CameraMaker,
            CameraModel = document.CameraModel,
            TakenOnUtc = document.TakenOnUtc,
            Orientation = document.Orientation,
            FocalLength = document.FocalLength,
            Aperture = document.Aperture,
            ExposureTime = document.ExposureTime,
            Iso = document.Iso,
            RawGpsLatitude = document.RawGpsLatitude,
            RawGpsLongitude = document.RawGpsLongitude,
        };
    }

    public static ImageExifMetadataDocument ToDocument(this ImageExifMetadata entity)
    {
        return new ImageExifMetadataDocument
        {
            CameraMaker = entity.CameraMaker,
            CameraModel = entity.CameraModel,
            TakenOnUtc = entity.TakenOnUtc,
            Orientation = entity.Orientation,
            FocalLength = entity.FocalLength,
            Aperture = entity.Aperture,
            ExposureTime = entity.ExposureTime,
            Iso = entity.Iso,
            RawGpsLatitude = entity.RawGpsLatitude,
            RawGpsLongitude = entity.RawGpsLongitude,
        };
    }

    public static ImageTag ToDomain(this ImageTagDocument document)
    {
        ImageTag entity = new ImageTag
        {
            Id = document.Id,
            Slug = document.Slug,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsActive = document.IsActive,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ImageTagDocument ToDocument(this ImageTag entity)
    {
        return new ImageTagDocument
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Labels = CommonMongoMappers.ToDocuments(entity.Labels),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

public static Country ToDomain(this CountryDocument document)
    {
        Country entity = new Country
        {
            Id = document.Id,
            IsoCode = document.IsoCode,
            Names = CommonMongoMappers.ToDomain(document.Names),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static CountryDocument ToDocument(this Country entity)
    {
        return new CountryDocument
        {
            Id = entity.Id,
            IsoCode = entity.IsoCode,
            Names = CommonMongoMappers.ToDocuments(entity.Names),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ParkFounder ToDomain(this ParkFounderDocument document)
    {
        ParkFounder entity = new ParkFounder
        {
            Id = document.Id,
            Name = document.Name,
            Occupation = document.Occupation,
            BirthDate = document.BirthDate,
            DeathDate = document.DeathDate,
            BirthPlace = document.BirthPlace,
            NationalityCountryCode = document.NationalityCountryCode,
            WebsiteUrl = document.WebsiteUrl,
            Biography = CommonMongoMappers.ToDomain(document.Biography),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkFounderDocument ToDocument(this ParkFounder entity)
    {
        return new ParkFounderDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            Occupation = entity.Occupation,
            BirthDate = entity.BirthDate,
            DeathDate = entity.DeathDate,
            BirthPlace = entity.BirthPlace,
            NationalityCountryCode = entity.NationalityCountryCode,
            WebsiteUrl = entity.WebsiteUrl,
            Biography = CommonMongoMappers.ToDocuments(entity.Biography),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ParkOperator ToDomain(this ParkOperatorDocument document)
    {
        ParkOperator entity = new ParkOperator
        {
            Id = document.Id,
            Name = document.Name,
            LegalName = document.LegalName,
            FoundedYear = document.FoundedYear,
            ClosedYear = document.ClosedYear,
            ContactDetails = ToDomainContactDetails(document.ContactDetails),
            Description = CommonMongoMappers.ToDomain(document.Description),
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkOperatorDocument ToDocument(this ParkOperator entity)
    {
        return new ParkOperatorDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            LegalName = entity.LegalName,
            FoundedYear = entity.FoundedYear,
            ClosedYear = entity.ClosedYear,
            ContactDetails = ToDocumentContactDetails(entity.ContactDetails),
            Description = CommonMongoMappers.ToDocuments(entity.Description),
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static AttractionManufacturer ToDomain(this AttractionManufacturerDocument document)
    {
        AttractionManufacturer entity = new AttractionManufacturer
        {
            Id = document.Id,
            Name = document.Name,
            LegalName = document.LegalName,
            FoundedYear = document.FoundedYear,
            ClosedYear = document.ClosedYear,
            ContactDetails = ToDomainContactDetails(document.ContactDetails),
            Biography = CommonMongoMappers.ToDomain(document.Biography),
            CurrentLogoImageId = document.CurrentLogoImageId,
            IsVisible = document.IsVisible,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static AttractionManufacturerDocument ToDocument(this AttractionManufacturer entity)
    {
        return new AttractionManufacturerDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            LegalName = entity.LegalName,
            FoundedYear = entity.FoundedYear,
            ClosedYear = entity.ClosedYear,
            ContactDetails = ToDocumentContactDetails(entity.ContactDetails),
            Biography = CommonMongoMappers.ToDocuments(entity.Biography),
            CurrentLogoImageId = entity.CurrentLogoImageId,
            IsVisible = entity.IsVisible,
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }
    private static ParkReferenceContactDetails? ToDomainContactDetails(ParkReferenceContactDetailsDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        return new ParkReferenceContactDetails
        {
            WebsiteUrl = document.WebsiteUrl,
            Email = document.Email,
            PhoneNumber = document.PhoneNumber,
            Street = document.Street,
            City = document.City,
            PostalCode = document.PostalCode,
            CountryCode = document.CountryCode,
            Latitude = document.Latitude,
            Longitude = document.Longitude,
        };
    }

    private static ParkReferenceContactDetailsDocument? ToDocumentContactDetails(ParkReferenceContactDetails? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ParkReferenceContactDetailsDocument
        {
            WebsiteUrl = entity.WebsiteUrl,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            Street = entity.Street,
            City = entity.City,
            PostalCode = entity.PostalCode,
            CountryCode = entity.CountryCode,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
        };
    }

private const string OpeningHoursTimeFormat = "HH:mm";

    public static ParkOpeningHoursScheduleDocument ToDocument(this ParkOpeningHoursSchedule schedule)
    {
        DateOnly? firstDate = ResolveFirstOpeningHoursDate(schedule);
        DateOnly? lastDate = ResolveLastOpeningHoursDate(schedule);

        return new ParkOpeningHoursScheduleDocument
        {
            Id = string.IsNullOrWhiteSpace(schedule.Id) ? Guid.NewGuid().ToString("N") : schedule.Id,
            ParkId = schedule.ParkId,
            TimeZoneId = schedule.TimeZoneId,
            SourceUrl = schedule.SourceUrl,
            Notes = schedule.Notes,
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            FirstDate = firstDate.HasValue ? FormatDate(firstDate.Value) : null,
            LastDate = lastDate.HasValue ? FormatDate(lastDate.Value) : null,
            HasScheduleData = schedule.RegularRules.Count > 0 || schedule.DateOverrides.Count > 0,
            CoverageSegments = schedule.CoverageSegments.Select(static segment => segment.ToDocument()).ToList(),
            CreatedAt = schedule.CreatedAtUtc,
            UpdatedAt = schedule.UpdatedAtUtc,
            RegularRules = schedule.RegularRules.Select(static rule => rule.ToDocument()).ToList(),
            DateOverrides = schedule.DateOverrides.Select(static dateOverride => dateOverride.ToDocument()).ToList(),
        };
    }

    public static ParkOpeningHoursSchedule ToDomain(this ParkOpeningHoursScheduleDocument document)
    {
        return new ParkOpeningHoursSchedule
        {
            Id = document.Id,
            ParkId = document.ParkId,
            TimeZoneId = document.TimeZoneId,
            SourceUrl = document.SourceUrl,
            Notes = document.Notes,
            LastVerifiedAtUtc = document.LastVerifiedAtUtc,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
            RegularRules = document.RegularRules.Select(static rule => rule.ToDomain()).ToList(),
            DateOverrides = document.DateOverrides.Select(static dateOverride => dateOverride.ToDomain()).ToList(),
            CoverageSegments = document.CoverageSegments.Select(static segment => segment.ToDomain()).ToList(),
        };
    }

    private static ParkOpeningHoursCoverageSegmentDocument ToDocument(this ParkOpeningHoursCoverageSegment segment)
    {
        return new ParkOpeningHoursCoverageSegmentDocument
        {
            StartDate = FormatDate(segment.StartDate),
            EndDate = FormatDate(segment.EndDate),
        };
    }

    private static ParkOpeningHoursCoverageSegment ToDomain(this ParkOpeningHoursCoverageSegmentDocument document)
    {
        return new ParkOpeningHoursCoverageSegment
        {
            StartDate = ParseDate(document.StartDate),
            EndDate = ParseDate(document.EndDate),
        };
    }

    private static ParkOpeningHoursRuleDocument ToDocument(this ParkOpeningHoursRule rule)
    {
        return new ParkOpeningHoursRuleDocument
        {
            Id = string.IsNullOrWhiteSpace(rule.Id) ? Guid.NewGuid().ToString("N") : rule.Id,
            StartDate = FormatDate(rule.StartDate),
            EndDate = FormatDate(rule.EndDate),
            DaysOfWeek = rule.DaysOfWeek.Select(static day => day.ToString()).ToList(),
            IsClosed = rule.IsClosed,
            Labels = CommonMongoMappers.ToDocuments(rule.Labels),
            Reasons = CommonMongoMappers.ToDocuments(rule.Reasons),
            SortOrder = rule.SortOrder,
            TimeRanges = rule.TimeRanges.Select(static timeRange => timeRange.ToDocument()).ToList(),
        };
    }

    private static ParkOpeningHoursRule ToDomain(this ParkOpeningHoursRuleDocument document)
    {
        return new ParkOpeningHoursRule
        {
            Id = document.Id,
            StartDate = ParseDate(document.StartDate),
            EndDate = ParseDate(document.EndDate),
            DaysOfWeek = document.DaysOfWeek
                .Select(static value => Enum.TryParse(value, true, out DayOfWeek parsed) ? parsed : (DayOfWeek?)null)
                .Where(static value => value.HasValue)
                .Select(static value => value!.Value)
                .ToList(),
            IsClosed = document.IsClosed,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Reasons = CommonMongoMappers.ToDomain(document.Reasons),
            SortOrder = document.SortOrder,
            TimeRanges = document.TimeRanges.Select(static timeRange => timeRange.ToDomain()).ToList(),
        };
    }

    private static ParkOpeningHoursDateOverrideDocument ToDocument(this ParkOpeningHoursDateOverride dateOverride)
    {
        return new ParkOpeningHoursDateOverrideDocument
        {
            LocalDate = FormatDate(dateOverride.LocalDate),
            IsClosed = dateOverride.IsClosed,
            Labels = CommonMongoMappers.ToDocuments(dateOverride.Labels),
            Reasons = CommonMongoMappers.ToDocuments(dateOverride.Reasons),
            TimeRanges = dateOverride.TimeRanges.Select(static timeRange => timeRange.ToDocument()).ToList(),
        };
    }

    private static ParkOpeningHoursDateOverride ToDomain(this ParkOpeningHoursDateOverrideDocument document)
    {
        return new ParkOpeningHoursDateOverride
        {
            LocalDate = ParseDate(document.LocalDate),
            IsClosed = document.IsClosed,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Reasons = CommonMongoMappers.ToDomain(document.Reasons),
            TimeRanges = document.TimeRanges.Select(static timeRange => timeRange.ToDomain()).ToList(),
        };
    }

    private static ParkOpeningHoursTimeRangeDocument ToDocument(this ParkOpeningHoursTimeRange timeRange)
    {
        return new ParkOpeningHoursTimeRangeDocument
        {
            OpensAt = FormatTime(timeRange.OpensAt),
            ClosesAt = FormatTime(timeRange.ClosesAt),
            ClosesNextDay = timeRange.ClosesNextDay,
            LastAdmissionAt = timeRange.LastAdmissionAt.HasValue ? FormatTime(timeRange.LastAdmissionAt.Value) : null,
            LastAdmissionNextDay = timeRange.LastAdmissionNextDay,
        };
    }

    private static ParkOpeningHoursTimeRange ToDomain(this ParkOpeningHoursTimeRangeDocument document)
    {
        return new ParkOpeningHoursTimeRange
        {
            OpensAt = ParseTime(document.OpensAt),
            ClosesAt = ParseTime(document.ClosesAt),
            ClosesNextDay = document.ClosesNextDay,
            LastAdmissionAt = string.IsNullOrWhiteSpace(document.LastAdmissionAt) ? null : ParseTime(document.LastAdmissionAt),
            LastAdmissionNextDay = document.LastAdmissionNextDay,
        };
    }

    private static string FormatTime(TimeOnly time)
    {
        return time.ToString(OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }

    private static TimeOnly ParseTime(string time)
    {
        return TimeOnly.ParseExact(time, OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }

    private static DateOnly? ResolveFirstOpeningHoursDate(ParkOpeningHoursSchedule schedule)
    {
        List<DateOnly> dates = new List<DateOnly>();
        dates.AddRange(schedule.RegularRules.Select(static rule => rule.StartDate));
        dates.AddRange(schedule.DateOverrides.Select(static dateOverride => dateOverride.LocalDate));
        return dates.Count == 0 ? null : dates.Min();
    }

    private static DateOnly? ResolveLastOpeningHoursDate(ParkOpeningHoursSchedule schedule)
    {
        List<DateOnly> dates = new List<DateOnly>();
        dates.AddRange(schedule.RegularRules.Select(static rule => rule.EndDate));
        dates.AddRange(schedule.DateOverrides.Select(static dateOverride => dateOverride.LocalDate));
        return dates.Count == 0 ? null : dates.Max();
    }

public static ParkPricingDocument ToDocument(this ParkPricingEntity pricing)
    {
        return new ParkPricingDocument
        {
            Id = string.IsNullOrWhiteSpace(pricing.Id) ? Guid.NewGuid().ToString("N") : pricing.Id,
            ParkId = pricing.ParkId,
            CurrencyCode = pricing.CurrencyCode,
            SourceUrl = pricing.SourceUrl,
            PurchaseUrl = pricing.PurchaseUrl,
            Notes = CommonMongoMappers.ToDocuments(pricing.Notes),
            LastVerifiedAtUtc = pricing.LastVerifiedAtUtc,
            CreatedAt = pricing.CreatedAtUtc,
            UpdatedAt = pricing.UpdatedAtUtc,
            AdmissionOffers = pricing.AdmissionOffers.Select(static offer => offer.ToDocument()).ToList(),
            AnnualPasses = pricing.AnnualPasses.Select(static offer => offer.ToDocument()).ToList(),
            ParkingOffers = pricing.ParkingOffers.Select(static offer => offer.ToDocument()).ToList(),
            CreditOffers = pricing.CreditOffers.Select(static offer => offer.ToDocument()).ToList(),
            HistoricalSnapshots = pricing.HistoricalSnapshots.Select(static snapshot => snapshot.ToDocument()).ToList(),
        };
    }

    public static ParkPricingEntity ToDomain(this ParkPricingDocument document)
    {
        return new ParkPricingEntity
        {
            Id = document.Id,
            ParkId = document.ParkId,
            CurrencyCode = document.CurrencyCode,
            SourceUrl = document.SourceUrl,
            PurchaseUrl = document.PurchaseUrl,
            Notes = CommonMongoMappers.ToDomain(document.Notes),
            LastVerifiedAtUtc = document.LastVerifiedAtUtc,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
            AdmissionOffers = document.AdmissionOffers.Select(static offer => offer.ToDomain()).ToList(),
            AnnualPasses = document.AnnualPasses.Select(static offer => offer.ToDomain()).ToList(),
            ParkingOffers = document.ParkingOffers.Select(static offer => offer.ToDomain()).ToList(),
            CreditOffers = document.CreditOffers.Select(static offer => offer.ToDomain()).ToList(),
            HistoricalSnapshots = document.HistoricalSnapshots.Select(static snapshot => snapshot.ToDomain()).ToList(),
        };
    }

    private static ParkPricingSnapshotDocument ToDocument(this ParkPricingSnapshot snapshot)
    {
        return new ParkPricingSnapshotDocument
        {
            Id = string.IsNullOrWhiteSpace(snapshot.Id) ? Guid.NewGuid().ToString("N") : snapshot.Id,
            Year = snapshot.Year,
            CurrencyCode = snapshot.CurrencyCode,
            SourceUrl = snapshot.SourceUrl,
            Notes = CommonMongoMappers.ToDocuments(snapshot.Notes),
            LastVerifiedAtUtc = snapshot.LastVerifiedAtUtc,
            AdmissionOffers = snapshot.AdmissionOffers.Select(static offer => offer.ToDocument()).ToList(),
            AnnualPasses = snapshot.AnnualPasses.Select(static offer => offer.ToDocument()).ToList(),
            ParkingOffers = snapshot.ParkingOffers.Select(static offer => offer.ToDocument()).ToList(),
            CreditOffers = snapshot.CreditOffers.Select(static offer => offer.ToDocument()).ToList(),
        };
    }

    private static ParkPricingSnapshot ToDomain(this ParkPricingSnapshotDocument document)
    {
        return new ParkPricingSnapshot
        {
            Id = document.Id,
            Year = document.Year,
            CurrencyCode = document.CurrencyCode,
            SourceUrl = document.SourceUrl,
            Notes = CommonMongoMappers.ToDomain(document.Notes),
            LastVerifiedAtUtc = document.LastVerifiedAtUtc,
            AdmissionOffers = document.AdmissionOffers.Select(static offer => offer.ToDomain()).ToList(),
            AnnualPasses = document.AnnualPasses.Select(static offer => offer.ToDomain()).ToList(),
            ParkingOffers = document.ParkingOffers.Select(static offer => offer.ToDomain()).ToList(),
            CreditOffers = document.CreditOffers.Select(static offer => offer.ToDomain()).ToList(),
        };
    }

    private static ParkAdmissionPriceOfferDocument ToDocument(this ParkAdmissionPriceOffer offer)
    {
        return new ParkAdmissionPriceOfferDocument
        {
            Id = string.IsNullOrWhiteSpace(offer.Id) ? Guid.NewGuid().ToString("N") : offer.Id,
            Code = offer.Code,
            AudienceCategory = offer.AudienceCategory,
            Labels = CommonMongoMappers.ToDocuments(offer.Labels),
            OnlinePrice = offer.OnlinePrice?.ToDocument(),
            GatePrice = offer.GatePrice?.ToDocument(),
            ValidFrom = FormatPricingDate(offer.ValidFrom),
            ValidTo = FormatPricingDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDocuments(offer.Conditions),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkAdmissionPriceOffer ToDomain(this ParkAdmissionPriceOfferDocument document)
    {
        return new ParkAdmissionPriceOffer
        {
            Id = document.Id,
            Code = document.Code,
            AudienceCategory = document.AudienceCategory,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            OnlinePrice = document.OnlinePrice?.ToDomain(),
            GatePrice = document.GatePrice?.ToDomain(),
            ValidFrom = ParsePricingDate(document.ValidFrom),
            ValidTo = ParsePricingDate(document.ValidTo),
            PurchaseUrl = document.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDomain(document.Conditions),
            SortOrder = document.SortOrder,
        };
    }

    private static ParkAnnualPassOfferDocument ToDocument(this ParkAnnualPassOffer offer)
    {
        return new ParkAnnualPassOfferDocument
        {
            Id = string.IsNullOrWhiteSpace(offer.Id) ? Guid.NewGuid().ToString("N") : offer.Id,
            Code = offer.Code,
            Names = CommonMongoMappers.ToDocuments(offer.Names),
            OnlinePrice = offer.OnlinePrice?.ToDocument(),
            GatePrice = offer.GatePrice?.ToDocument(),
            ValidFrom = FormatPricingDate(offer.ValidFrom),
            ValidTo = FormatPricingDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDocuments(offer.Conditions),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkAnnualPassOffer ToDomain(this ParkAnnualPassOfferDocument document)
    {
        return new ParkAnnualPassOffer
        {
            Id = document.Id,
            Code = document.Code,
            Names = CommonMongoMappers.ToDomain(document.Names),
            OnlinePrice = document.OnlinePrice?.ToDomain(),
            GatePrice = document.GatePrice?.ToDomain(),
            ValidFrom = ParsePricingDate(document.ValidFrom),
            ValidTo = ParsePricingDate(document.ValidTo),
            PurchaseUrl = document.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDomain(document.Conditions),
            SortOrder = document.SortOrder,
        };
    }

    private static ParkParkingPriceOfferDocument ToDocument(this ParkParkingPriceOffer offer)
    {
        return new ParkParkingPriceOfferDocument
        {
            Id = string.IsNullOrWhiteSpace(offer.Id) ? Guid.NewGuid().ToString("N") : offer.Id,
            Code = offer.Code,
            Labels = CommonMongoMappers.ToDocuments(offer.Labels),
            OnlinePrice = offer.OnlinePrice?.ToDocument(),
            GatePrice = offer.GatePrice?.ToDocument(),
            ValidFrom = FormatPricingDate(offer.ValidFrom),
            ValidTo = FormatPricingDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDocuments(offer.Conditions),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkParkingPriceOffer ToDomain(this ParkParkingPriceOfferDocument document)
    {
        return new ParkParkingPriceOffer
        {
            Id = document.Id,
            Code = document.Code,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            OnlinePrice = document.OnlinePrice?.ToDomain(),
            GatePrice = document.GatePrice?.ToDomain(),
            ValidFrom = ParsePricingDate(document.ValidFrom),
            ValidTo = ParsePricingDate(document.ValidTo),
            PurchaseUrl = document.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDomain(document.Conditions),
            SortOrder = document.SortOrder,
        };
    }

    private static ParkCreditOfferDocument ToDocument(this ParkCreditOffer offer)
    {
        return new ParkCreditOfferDocument
        {
            Id = string.IsNullOrWhiteSpace(offer.Id) ? Guid.NewGuid().ToString("N") : offer.Id,
            UnitCode = offer.UnitCode,
            Quantity = offer.Quantity,
            Labels = CommonMongoMappers.ToDocuments(offer.Labels),
            Prices = new ParkCreditOfferPricesDocument
            {
                OnlinePrice = offer.Prices.OnlinePrice,
                GatePrice = offer.Prices.GatePrice,
            },
            ValidFrom = FormatPricingDate(offer.ValidFrom),
            ValidTo = FormatPricingDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDocuments(offer.Conditions),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkCreditOffer ToDomain(this ParkCreditOfferDocument document)
    {
        return new ParkCreditOffer
        {
            Id = document.Id,
            UnitCode = document.UnitCode,
            Quantity = document.Quantity,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Prices = new ParkCreditOfferPrices
            {
                OnlinePrice = document.Prices?.OnlinePrice,
                GatePrice = document.Prices?.GatePrice,
            },
            ValidFrom = ParsePricingDate(document.ValidFrom),
            ValidTo = ParsePricingDate(document.ValidTo),
            PurchaseUrl = document.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDomain(document.Conditions),
            SortOrder = document.SortOrder,
        };
    }

    private static ParkPriceValueDocument ToDocument(this ParkPriceValue price)
    {
        return new ParkPriceValueDocument
        {
            Mode = price.Mode.ToString(),
            Amount = price.Amount,
            MinimumAmount = price.MinimumAmount,
            MaximumAmount = price.MaximumAmount,
        };
    }

    private static ParkPriceValue ToDomain(this ParkPriceValueDocument document)
    {
        return new ParkPriceValue
        {
            Mode = Enum.TryParse(document.Mode, true, out ParkPricingMode mode) && Enum.IsDefined(mode) ? mode : ParkPricingMode.Dynamic,
            Amount = document.Amount,
            MinimumAmount = document.MinimumAmount,
            MaximumAmount = document.MaximumAmount,
        };
    }

    private static string? FormatPricingDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParsePricingDate(string? value)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : null;
    }

public static Park ToDomain(this ParkDocument document)
    {
        Park entity = new Park
        {
            Id = document.Id,
            Name = document.Name,
            CountryCode = document.CountryCode,
            Type = document.Type,
            AudienceClassification = document.AudienceClassification,
            Status = document.Status,
            OpeningDate = document.OpeningDate,
            ClosingDate = document.ClosingDate,
            OpeningDateText = document.OpeningDateText,
            ClosingDateText = document.ClosingDateText,
            FounderId = document.FounderId,
            OperatorId = document.OperatorId,
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsVisible = document.IsVisible,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
            IsFeaturedOnHome = document.IsFeaturedOnHome,
            FeaturedHomeOrder = document.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = document.IsFeaturedOnHomeSponsored,
            WebsiteUrl = document.WebsiteUrl,
            Street = document.Street,
            City = document.City,
            PostalCode = document.PostalCode,
            CurrentLogoImageId = document.CurrentLogoImageId,
            OfficialMaps = (document.OfficialMaps ?? new List<ParkOfficialMapDocument>())
                .Select(static officialMap => officialMap.ToDomain())
                .ToList(),
        };

        CommonMongoMappers.ApplyPosition(entity, document.Latitude, document.Longitude);
        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkDocument ToDocument(this Park entity)
    {
        ParkDocument document = new ParkDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            CountryCode = entity.CountryCode,
            Type = entity.Type,
            AudienceClassification = entity.AudienceClassification,
            Status = entity.Status,
            OpeningDate = entity.OpeningDate,
            ClosingDate = entity.ClosingDate,
            OpeningDateText = entity.OpeningDateText,
            ClosingDateText = entity.ClosingDateText,
            FounderId = entity.FounderId,
            OperatorId = entity.OperatorId,
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsVisible = entity.IsVisible,
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            IsFeaturedOnHome = entity.IsFeaturedOnHome,
            FeaturedHomeOrder = entity.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = entity.IsFeaturedOnHomeSponsored,
            WebsiteUrl = entity.WebsiteUrl,
            Street = entity.Street,
            City = entity.City,
            PostalCode = entity.PostalCode,
            CurrentLogoImageId = entity.CurrentLogoImageId,
            OfficialMaps = (entity.OfficialMaps ?? new List<ParkOfficialMap>())
                .Select(static officialMap => officialMap.ToDocument())
                .ToList(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };

        CommonMongoMappers.ApplyPosition(document, entity.Position);
        return document;
    }

    internal static ParkOfficialMap ToDomain(this ParkOfficialMapDocument document)
    {
        return new ParkOfficialMap
        {
            Id = document.Id,
            Year = document.Year,
            Format = document.Format,
            DocumentUrl = document.DocumentUrl,
            StorageKey = document.StorageKey,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            SizeInBytes = document.SizeInBytes,
            PreviewImageUrl = document.PreviewImageUrl,
            SourcePageUrl = document.SourcePageUrl,
            LanguageCode = document.LanguageCode,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            AlternativeTexts = CommonMongoMappers.ToDomain(document.AlternativeTexts),
            IsVisible = document.IsVisible,
            LastVerifiedAtUtc = document.LastVerifiedAtUtc,
        };
    }

    private static ParkOfficialMapDocument ToDocument(this ParkOfficialMap entity)
    {
        return new ParkOfficialMapDocument
        {
            Id = entity.Id,
            Year = entity.Year,
            Format = entity.Format,
            DocumentUrl = entity.DocumentUrl,
            StorageKey = entity.StorageKey,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            SizeInBytes = entity.SizeInBytes,
            PreviewImageUrl = entity.PreviewImageUrl,
            SourcePageUrl = entity.SourcePageUrl,
            LanguageCode = entity.LanguageCode,
            Titles = CommonMongoMappers.ToDocuments(entity.Titles),
            AlternativeTexts = CommonMongoMappers.ToDocuments(entity.AlternativeTexts),
            IsVisible = entity.IsVisible,
            LastVerifiedAtUtc = entity.LastVerifiedAtUtc,
        };
    }

    public static ParkZone ToDomain(this ParkZoneDocument document)
    {
        ParkZone entity = new ParkZone
        {
            Id = document.Id,
            ParkId = document.ParkId,
            Name = document.Name,
            Names = CommonMongoMappers.ToDomain(document.Names),
            Slug = document.Slug,
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsVisible = document.IsVisible,
            SortOrder = document.SortOrder,
        };

        CommonMongoMappers.ApplyPosition(entity, document.Latitude, document.Longitude);
        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkZoneDocument ToDocument(this ParkZone entity)
    {
        ParkZoneDocument document = new ParkZoneDocument
        {
            Id = entity.Id,
            ParkId = entity.ParkId,
            Name = entity.Name,
            Names = CommonMongoMappers.ToDocuments(entity.Names),
            Slug = entity.Slug,
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsVisible = entity.IsVisible,
            SortOrder = entity.SortOrder,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };

        CommonMongoMappers.ApplyPosition(document, entity.Position);
        return document;
    }

    public static ParkItem ToDomain(this ParkItemDocument document)
    {
        ParkItem entity = new ParkItem
        {
            Id = document.Id,
            ParkId = document.ParkId,
            ZoneId = document.ZoneId,
            Name = document.Name,
            Category = document.Category,
            Type = document.Type,
            Subtype = document.Subtype,
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            AttractionDetails = document.AttractionDetails?.ToDomain(),
            AttractionLocations = document.AttractionLocations?.ToDomain(),
            IsVisible = document.IsVisible,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
        };

        CommonMongoMappers.ApplyPosition(entity, document.Latitude, document.Longitude);
        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkItemDocument ToDocument(this ParkItem entity)
    {
        ParkItemDocument document = new ParkItemDocument
        {
            Id = entity.Id,
            ParkId = entity.ParkId,
            ZoneId = entity.ZoneId,
            Name = entity.Name,
            Category = entity.Category,
            Type = entity.Type,
            Subtype = entity.Subtype,
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            AttractionDetails = entity.AttractionDetails?.ToDocument(),
            AttractionLocations = entity.AttractionLocations?.ToDocument(),
            IsVisible = entity.IsVisible,
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };

        CommonMongoMappers.ApplyPosition(document, entity.Position);
        return document;
    }

    public static StandaloneAttraction ToDomain(this StandaloneAttractionDocument document)
    {
        StandaloneAttraction entity = new StandaloneAttraction
        {
            Id = document.Id,
            Name = document.Name,
            CountryCode = document.CountryCode,
            Type = document.Type,
            Subtype = document.Subtype,
            OperatorId = document.OperatorId,
            WebsiteUrl = document.WebsiteUrl,
            Street = document.Street,
            City = document.City,
            PostalCode = document.PostalCode,
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            AttractionDetails = document.AttractionDetails?.ToDomain(),
            AttractionLocations = document.AttractionLocations?.ToDomain(),
            IsVisible = document.IsVisible,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
            LegacyParkId = document.LegacyParkId,
            LegacyParkItemId = document.LegacyParkItemId,
        };

        CommonMongoMappers.ApplyPosition(entity, document.Latitude, document.Longitude);
        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static StandaloneAttractionDocument ToDocument(this StandaloneAttraction entity)
    {
        StandaloneAttractionDocument document = new StandaloneAttractionDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            CountryCode = entity.CountryCode,
            Type = entity.Type,
            Subtype = entity.Subtype,
            OperatorId = entity.OperatorId,
            WebsiteUrl = entity.WebsiteUrl,
            Street = entity.Street,
            City = entity.City,
            PostalCode = entity.PostalCode,
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            AttractionDetails = entity.AttractionDetails?.ToDocument(),
            AttractionLocations = entity.AttractionLocations?.ToDocument(),
            IsVisible = entity.IsVisible,
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            LegacyParkId = entity.LegacyParkId,
            LegacyParkItemId = entity.LegacyParkItemId,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };

        CommonMongoMappers.ApplyPosition(document, entity.Position);
        return document;
    }

    public static AttractionDetails ToDomain(this AttractionDetailsDocument document)
    {
        AttractionDetails entity = new AttractionDetails
        {
            ManufacturerId = document.ManufacturerId,
            Model = document.Model,
            ExternalSource = document.ExternalSource,
            ExternalId = document.ExternalId,
            SourceUrl = document.SourceUrl,
            Status = ParkItemStatusNormalizer.Normalize(document.Status),
            MaterialType = document.MaterialType,
            SeatingType = document.SeatingType,
            LaunchType = document.LaunchType,
            RestraintType = document.RestraintType,
            IsLaunched = document.IsLaunched,
            OpeningDate = document.OpeningDate,
            ClosingDate = document.ClosingDate,
            OpeningDateText = document.OpeningDateText,
            ClosingDateText = document.ClosingDateText,
            DurationInSeconds = document.DurationInSeconds,
            CapacityPerHour = document.CapacityPerHour,
            HeightInFeet = document.HeightInFeet,
            HeightInMeters = document.HeightInMeters,
            LengthInFeet = document.LengthInFeet,
            LengthInMeters = document.LengthInMeters,
            SpeedInMph = document.SpeedInMph,
            SpeedInKmH = document.SpeedInKmH,
            DropInFeet = document.DropInFeet,
            DropInMeters = document.DropInMeters,
            InversionCount = document.InversionCount,
            TrainCount = document.TrainCount,
            CarsPerTrain = document.CarsPerTrain,
            RidersPerVehicle = document.RidersPerVehicle,
            HasSingleRider = document.HasSingleRider,
            HasFastPass = document.HasFastPass,
            IsAccessibleForReducedMobility = document.IsAccessibleForReducedMobility,
            IsIndoor = document.IsIndoor,
            WaterExposureLevel = document.WaterExposureLevel,
            AccessConditions = document.AccessConditions.Select(ToDomain).ToList(),
        };

        return entity;
    }

    public static AttractionDetailsDocument ToDocument(this AttractionDetails entity)
    {
        return new AttractionDetailsDocument
        {
            ManufacturerId = entity.ManufacturerId,
            Model = entity.Model,
            ExternalSource = entity.ExternalSource,
            ExternalId = entity.ExternalId,
            SourceUrl = entity.SourceUrl,
            Status = ParkItemStatusNormalizer.Normalize(entity.Status),
            MaterialType = entity.MaterialType,
            SeatingType = entity.SeatingType,
            LaunchType = entity.LaunchType,
            RestraintType = entity.RestraintType,
            IsLaunched = entity.IsLaunched,
            OpeningDate = entity.OpeningDate,
            ClosingDate = entity.ClosingDate,
            OpeningDateText = entity.OpeningDateText,
            ClosingDateText = entity.ClosingDateText,
            DurationInSeconds = entity.DurationInSeconds,
            CapacityPerHour = entity.CapacityPerHour,
            HeightInFeet = entity.HeightInFeet,
            HeightInMeters = entity.HeightInMeters,
            LengthInFeet = entity.LengthInFeet,
            LengthInMeters = entity.LengthInMeters,
            SpeedInMph = entity.SpeedInMph,
            SpeedInKmH = entity.SpeedInKmH,
            DropInFeet = entity.DropInFeet,
            DropInMeters = entity.DropInMeters,
            InversionCount = entity.InversionCount,
            TrainCount = entity.TrainCount,
            CarsPerTrain = entity.CarsPerTrain,
            RidersPerVehicle = entity.RidersPerVehicle,
            HasSingleRider = entity.HasSingleRider,
            HasFastPass = entity.HasFastPass,
            IsAccessibleForReducedMobility = entity.IsAccessibleForReducedMobility,
            IsIndoor = entity.IsIndoor,
            WaterExposureLevel = entity.WaterExposureLevel,
            AccessConditions = entity.AccessConditions.Select(ToDocument).ToList(),
        };
    }

    public static AttractionAccessCondition ToDomain(this AttractionAccessConditionDocument document)
    {
        return new AttractionAccessCondition
        {
            Type = document.Type,
            TypeKey = document.TypeKey,
            IsCustom = document.IsCustom,
            CustomTypeKey = document.CustomTypeKey,
            CustomTypeLabel = CommonMongoMappers.ToDomain(document.CustomTypeLabel),
            Value = document.Value,
            Unit = document.Unit,
            RequiresAccompaniment = document.RequiresAccompaniment,
            MinimumCompanionAge = document.MinimumCompanionAge,
            Label = CommonMongoMappers.ToDomain(document.Label),
            Description = CommonMongoMappers.ToDomain(document.Description),
            DisplayOrder = document.DisplayOrder,
            ProvenanceSchemaVersion = document.ProvenanceSchemaVersion,
            SourceKind = document.SourceKind,
            SourceUrl = document.SourceUrl,
            SourceReference = document.SourceReference,
            CollectedAtUtc = document.CollectedAtUtc,
            VerifiedAtUtc = document.VerifiedAtUtc,
            SourceLanguageCode = document.SourceLanguageCode,
            SourceSummary = CommonMongoMappers.ToDomain(document.SourceSummary),
            SourceConfidence = document.SourceConfidence,
            Scope = document.Scope,
            ScopeDetail = document.ScopeDetail,
            EffectiveFrom = ParseAccessConditionDate(document.EffectiveFrom),
            EffectiveTo = ParseAccessConditionDate(document.EffectiveTo),
        };
    }

    public static AttractionAccessConditionDocument ToDocument(this AttractionAccessCondition entity)
    {
        return new AttractionAccessConditionDocument
        {
            Type = entity.Type,
            TypeKey = entity.TypeKey,
            IsCustom = entity.IsCustom,
            CustomTypeKey = entity.CustomTypeKey,
            CustomTypeLabel = CommonMongoMappers.ToDocuments(entity.CustomTypeLabel),
            Value = entity.Value,
            Unit = entity.Unit,
            RequiresAccompaniment = entity.RequiresAccompaniment,
            MinimumCompanionAge = entity.MinimumCompanionAge,
            Label = CommonMongoMappers.ToDocuments(entity.Label),
            Description = CommonMongoMappers.ToDocuments(entity.Description),
            DisplayOrder = entity.DisplayOrder,
            ProvenanceSchemaVersion = entity.ProvenanceSchemaVersion,
            SourceKind = entity.SourceKind,
            SourceUrl = entity.SourceUrl,
            SourceReference = entity.SourceReference,
            CollectedAtUtc = entity.CollectedAtUtc,
            VerifiedAtUtc = entity.VerifiedAtUtc,
            SourceLanguageCode = entity.SourceLanguageCode,
            SourceSummary = CommonMongoMappers.ToDocuments(entity.SourceSummary),
            SourceConfidence = entity.SourceConfidence,
            Scope = entity.Scope,
            ScopeDetail = entity.ScopeDetail,
            EffectiveFrom = FormatAccessConditionDate(entity.EffectiveFrom),
            EffectiveTo = FormatAccessConditionDate(entity.EffectiveTo),
        };
    }

    private static string? FormatAccessConditionDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParseAccessConditionDate(string? value)
    {
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly parsed)
                ? parsed
                : null;
    }

    public static AttractionLocations ToDomain(this AttractionLocationsDocument document)
    {
        return new AttractionLocations
        {
            Entrance = CommonMongoMappers.ToDomain(document.Entrance),
            Exit = CommonMongoMappers.ToDomain(document.Exit),
            FastPassEntrance = CommonMongoMappers.ToDomain(document.FastPassEntrance),
            ReducedMobilityEntrance = CommonMongoMappers.ToDomain(document.ReducedMobilityEntrance),
        };
    }

    public static AttractionLocationsDocument ToDocument(this AttractionLocations entity)
    {
        return new AttractionLocationsDocument
        {
            Entrance = CommonMongoMappers.ToDocument(entity.Entrance),
            Exit = CommonMongoMappers.ToDocument(entity.Exit),
            FastPassEntrance = CommonMongoMappers.ToDocument(entity.FastPassEntrance),
            ReducedMobilityEntrance = CommonMongoMappers.ToDocument(entity.ReducedMobilityEntrance),
        };
    }
    public static AttractionAccessConditionTypeDefinition ToDomain(this AttractionAccessConditionTypeDefinitionDocument document)
    {
        return new AttractionAccessConditionTypeDefinition
        {
            Id = document.Id,
            Key = document.Key,
            LegacyType = document.LegacyType,
            IsSystem = document.IsSystem,
            IsActive = document.IsActive,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            SortOrder = document.SortOrder,
        };
    }

    public static AttractionAccessConditionTypeDefinitionDocument ToDocument(this AttractionAccessConditionTypeDefinition entity)
    {
        return new AttractionAccessConditionTypeDefinitionDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            Key = entity.Key,
            LegacyType = entity.LegacyType,
            IsSystem = entity.IsSystem,
            IsActive = entity.IsActive,
            Labels = CommonMongoMappers.ToDocuments(entity.Labels),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            SortOrder = entity.SortOrder,
        };
    }

public static UserRating ToDomain(this UserRatingDocument document)
    {
        return new UserRating
        {
            Id = document.Id,
            UserId = document.UserId,
            TargetType = document.TargetType,
            TargetId = document.TargetId,
            ParkId = document.ParkId,
            ParkItemCategory = document.ParkItemCategory,
            ParkItemType = document.ParkItemType,
            Value = document.Value,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
        };
    }

    public static UserRatingDocument ToDocument(this UserRating entity)
    {
        return new UserRatingDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            UserId = entity.UserId,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            ParkId = entity.ParkId,
            ParkItemCategory = entity.ParkItemCategory,
            ParkItemType = entity.ParkItemType,
            Value = entity.Value,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static RatingAggregate ToDomain(this RatingAggregateDocument document)
    {
        return new RatingAggregate
        {
            Id = document.Id,
            TargetType = document.TargetType,
            TargetId = document.TargetId,
            ParkId = document.ParkId,
            ParkItemCategory = document.ParkItemCategory,
            ParkItemType = document.ParkItemType,
            RatingCount = document.RatingCount,
            UniqueContributorCount = document.UniqueContributorCount,
            RatingSum = document.RatingSum,
            AverageRating = document.AverageRating,
            BayesianScore = document.BayesianScore,
            LastRatedAtUtc = document.LastRatedAtUtc,
            MutationVersion = document.MutationVersion,
            CalculatedVersion = document.CalculatedVersion,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
        };
    }

    public static RatingAggregateDocument ToDocument(this RatingAggregate entity)
    {
        return new RatingAggregateDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            ParkId = entity.ParkId,
            ParkItemCategory = entity.ParkItemCategory,
            ParkItemType = entity.ParkItemType,
            RatingCount = entity.RatingCount,
            UniqueContributorCount = entity.UniqueContributorCount,
            RatingSum = entity.RatingSum,
            AverageRating = entity.AverageRating,
            BayesianScore = entity.BayesianScore,
            LastRatedAtUtc = entity.LastRatedAtUtc,
            MutationVersion = entity.MutationVersion
                ?? throw new InvalidOperationException("Rating aggregate mutation version is required for persistence."),
            CalculatedVersion = entity.CalculatedVersion
                ?? throw new InvalidOperationException("Rating aggregate calculation version is required for persistence."),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

public static SocialPublication ToDomain(this SocialPublicationDocument document)
    {
        return new SocialPublication
        {
            Id = document.Id,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
            Network = ParseEnumOrDefault(document.Network, SocialNetwork.Facebook),
            Status = ParseEnumOrDefault(document.Status, SocialPublicationStatus.Pending),
            Trigger = ParseEnumOrDefault(document.Trigger, SocialPublicationTrigger.Manual),
            Message = document.Message,
            Url = document.Url,
            SourceEntityType = document.SourceEntityType,
            SourceEntityId = document.SourceEntityId,
            RequestedByUserId = document.RequestedByUserId,
            DeduplicationKey = document.DeduplicationKey,
            RequestedAtUtc = document.RequestedAtUtc,
            AttemptedAtUtc = document.AttemptedAtUtc,
            PublishedAtUtc = document.PublishedAtUtc,
            DeletedAtUtc = document.DeletedAtUtc,
            LastSynchronizedAtUtc = document.LastSynchronizedAtUtc,
            ExternalPostId = document.ExternalPostId,
            ExternalPostUrl = document.ExternalPostUrl,
            FailureCode = document.FailureCode,
            FailureMessage = document.FailureMessage,
        };
    }

    public static SocialPublicationDocument ToDocument(this SocialPublication entity)
    {
        return new SocialPublicationDocument
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
            Network = entity.Network.ToString(),
            Status = entity.Status.ToString(),
            Trigger = entity.Trigger.ToString(),
            Message = entity.Message,
            Url = entity.Url,
            SourceEntityType = entity.SourceEntityType,
            SourceEntityId = entity.SourceEntityId,
            RequestedByUserId = entity.RequestedByUserId,
            DeduplicationKey = entity.DeduplicationKey,
            RequestedAtUtc = entity.RequestedAtUtc,
            AttemptedAtUtc = entity.AttemptedAtUtc,
            PublishedAtUtc = entity.PublishedAtUtc,
            DeletedAtUtc = entity.DeletedAtUtc,
            LastSynchronizedAtUtc = entity.LastSynchronizedAtUtc,
            ExternalPostId = entity.ExternalPostId,
            ExternalPostUrl = entity.ExternalPostUrl,
            FailureCode = entity.FailureCode,
            FailureMessage = entity.FailureMessage,
        };
    }

public static SocialShareEvent ToDomain(this SocialShareEventDocument document)
    {
        SocialShareEvent entity = new SocialShareEvent
        {
            Id = document.Id,
            OccurredAtUtc = document.OccurredAtUtc,
            TargetType = ParseEnumOrDefault(document.TargetType, SocialShareTargetType.Page),
            TargetId = document.TargetId,
            TargetTitle = document.TargetTitle,
            Url = document.Url,
            LanguageCode = document.LanguageCode,
            Channel = ParseEnumOrDefault(document.Channel, SocialShareChannel.Copy),
            VisitorKind = ParseEnumOrDefault(document.VisitorKind, SocialShareVisitorKind.Anonymous),
            UserId = document.UserId,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static SocialShareEventDocument ToDocument(this SocialShareEvent entity)
    {
        return new SocialShareEventDocument
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
            OccurredAtUtc = entity.OccurredAtUtc,
            TargetType = entity.TargetType.ToString(),
            TargetId = entity.TargetId,
            TargetTitle = entity.TargetTitle,
            Url = entity.Url,
            LanguageCode = entity.LanguageCode,
            Channel = entity.Channel.ToString(),
            VisitorKind = entity.VisitorKind.ToString(),
            UserId = entity.UserId,
        };
    }

    private static TEnum ParseEnumOrDefault<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
    }

public static TechnicalPage ToDomain(this TechnicalPageDocument document)
    {
        TechnicalPage page = new TechnicalPage
        {
            Id = document.Id,
            CategoryKey = document.CategoryKey,
            CategoryNames = CommonMongoMappers.ToDomain(document.CategoryNames),
            Slug = document.Slug,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Summaries = CommonMongoMappers.ToDomain(document.Summaries),
            Aliases = document.Aliases.Select(ToDomain).ToList(),
            ContentBlocks = document.ContentBlocks.Select(ToDomain).ToList(),
            SortOrder = document.SortOrder,
            IsVisible = document.IsVisible,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
        };
        page.CreatedAtUtc = document.CreatedAt;
        page.UpdatedAtUtc = document.UpdatedAt;
        return page;
    }

    public static TechnicalPageDocument ToDocument(this TechnicalPage page)
    {
        return new TechnicalPageDocument
        {
            Id = page.Id,
            CategoryKey = page.CategoryKey,
            CategoryNames = CommonMongoMappers.ToDocuments(page.CategoryNames),
            Slug = page.Slug,
            Titles = CommonMongoMappers.ToDocuments(page.Titles),
            Summaries = CommonMongoMappers.ToDocuments(page.Summaries),
            Aliases = page.Aliases.Select(ToDocument).ToList(),
            ContentBlocks = page.ContentBlocks.Select(ToDocument).ToList(),
            SortOrder = page.SortOrder,
            IsVisible = page.IsVisible,
            AdminReviewStatus = page.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = page.AdminReviewStatus.ToAdminReviewPriority(),
            CreatedAt = page.CreatedAtUtc,
            UpdatedAt = page.UpdatedAtUtc,
        };
    }

    private static TechnicalPageAlias ToDomain(TechnicalPageAliasDocument document)
    {
        return new TechnicalPageAlias
        {
            CategoryKey = document.CategoryKey,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
        };
    }

    private static TechnicalPageAliasDocument ToDocument(TechnicalPageAlias alias)
    {
        return new TechnicalPageAliasDocument
        {
            CategoryKey = alias.CategoryKey,
            Labels = CommonMongoMappers.ToDocuments(alias.Labels),
        };
    }

    private static TechnicalContentBlock ToDomain(TechnicalContentBlockDocument document)
    {
        return new TechnicalContentBlock
        {
            BlockType = document.BlockType,
            Tone = document.Tone,
            ImageUrl = document.ImageUrl,
            ImageId = document.ImageId,
            DiagramKey = document.DiagramKey,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Bodies = CommonMongoMappers.ToDomain(document.Bodies),
            Captions = CommonMongoMappers.ToDomain(document.Captions),
            AltTexts = CommonMongoMappers.ToDomain(document.AltTexts),
            Items = document.Items.Select(ToDomain).ToList(),
            Table = document.Table is null ? null : ToDomain(document.Table),
            Metrics = document.Metrics.Select(ToDomain).ToList(),
            Links = document.Links.Select(ToDomain).ToList(),
            Columns = document.Columns.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentBlockDocument ToDocument(TechnicalContentBlock block)
    {
        return new TechnicalContentBlockDocument
        {
            BlockType = block.BlockType,
            Tone = block.Tone,
            ImageUrl = block.ImageUrl,
            ImageId = block.ImageId,
            DiagramKey = block.DiagramKey,
            Titles = CommonMongoMappers.ToDocuments(block.Titles),
            Bodies = CommonMongoMappers.ToDocuments(block.Bodies),
            Captions = CommonMongoMappers.ToDocuments(block.Captions),
            AltTexts = CommonMongoMappers.ToDocuments(block.AltTexts),
            Items = block.Items.Select(ToDocument).ToList(),
            Table = block.Table is null ? null : ToDocument(block.Table),
            Metrics = block.Metrics.Select(ToDocument).ToList(),
            Links = block.Links.Select(ToDocument).ToList(),
            Columns = block.Columns.Select(ToDocument).ToList(),
        };
    }

    private static TechnicalContentListItem ToDomain(TechnicalContentListItemDocument document)
    {
        return new TechnicalContentListItem
        {
            Texts = CommonMongoMappers.ToDomain(document.Texts),
        };
    }

    private static TechnicalContentListItemDocument ToDocument(TechnicalContentListItem item)
    {
        return new TechnicalContentListItemDocument
        {
            Texts = CommonMongoMappers.ToDocuments(item.Texts),
        };
    }

    private static TechnicalContentTable ToDomain(TechnicalContentTableDocument document)
    {
        return new TechnicalContentTable
        {
            Headers = document.Headers.Select(ToDomain).ToList(),
            Rows = document.Rows.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentTableDocument ToDocument(TechnicalContentTable table)
    {
        return new TechnicalContentTableDocument
        {
            Headers = table.Headers.Select(ToDocument).ToList(),
            Rows = table.Rows.Select(ToDocument).ToList(),
        };
    }

    private static TechnicalContentTableRow ToDomain(TechnicalContentTableRowDocument document)
    {
        return new TechnicalContentTableRow
        {
            Cells = document.Cells.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentTableRowDocument ToDocument(TechnicalContentTableRow row)
    {
        return new TechnicalContentTableRowDocument
        {
            Cells = row.Cells.Select(ToDocument).ToList(),
        };
    }

    private static TechnicalContentTableCell ToDomain(TechnicalContentTableCellDocument document)
    {
        return new TechnicalContentTableCell
        {
            Texts = CommonMongoMappers.ToDomain(document.Texts),
        };
    }

    private static TechnicalContentTableCellDocument ToDocument(TechnicalContentTableCell cell)
    {
        return new TechnicalContentTableCellDocument
        {
            Texts = CommonMongoMappers.ToDocuments(cell.Texts),
        };
    }

    private static TechnicalContentMetric ToDomain(TechnicalContentMetricDocument document)
    {
        return new TechnicalContentMetric
        {
            Label = CommonMongoMappers.ToDomain(document.Label),
            Value = CommonMongoMappers.ToDomain(document.Value),
            HelpText = CommonMongoMappers.ToDomain(document.HelpText),
        };
    }

    private static TechnicalContentMetricDocument ToDocument(TechnicalContentMetric metric)
    {
        return new TechnicalContentMetricDocument
        {
            Label = CommonMongoMappers.ToDocuments(metric.Label),
            Value = CommonMongoMappers.ToDocuments(metric.Value),
            HelpText = CommonMongoMappers.ToDocuments(metric.HelpText),
        };
    }

    private static TechnicalContentLink ToDomain(TechnicalContentLinkDocument document)
    {
        return new TechnicalContentLink
        {
            Url = document.Url,
            Label = CommonMongoMappers.ToDomain(document.Label),
        };
    }

    private static TechnicalContentLinkDocument ToDocument(TechnicalContentLink link)
    {
        return new TechnicalContentLinkDocument
        {
            Url = link.Url,
            Label = CommonMongoMappers.ToDocuments(link.Label),
        };
    }

public static User ToDomain(this UserDocument document)
    {
        User entity = new User
        {
            Id = document.Id,
            FirstName = document.FirstName,
            LastName = document.LastName,
            PublicDisplayName = document.PublicDisplayName,
            UsesAutomaticPublicDisplayName = document.UsesAutomaticPublicDisplayName,
            Email = document.Email,
            HashedPassword = document.HashedPassword,
            IsActivated = document.IsActivated,
            IsBlocked = document.IsBlocked,
            PreferredLanguage = document.PreferredLanguage,
            PreferredMeasurementSystem = document.PreferredMeasurementSystem,
            AvatarUrl = document.AvatarUrl,
            Roles = document.Roles.ToList(),
            ExternalLogins = document.ExternalLogins.Select(ToDomain).ToList(),
            LastLoginUtc = document.LastLoginUtc,
            LastActivityUtc = document.LastActivityUtc,
            EmailConfirmationTokenHash = document.EmailConfirmationTokenHash,
            EmailConfirmationTokenExpiresAtUtc = document.EmailConfirmationTokenExpiresAtUtc,
            EmailConfirmationSentAtUtc = document.EmailConfirmationSentAtUtc,
            PasswordResetTokenHash = document.PasswordResetTokenHash,
            PasswordResetTokenExpiresAtUtc = document.PasswordResetTokenExpiresAtUtc,
            PasswordResetSentAtUtc = document.PasswordResetSentAtUtc,
        };

        if (document.PublicAccountNumber > 0)
        {
            entity.AssignPublicAccountNumber(document.PublicAccountNumber);
        }

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static UserDocument ToDocument(this User entity)
    {
        return new UserDocument
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            PublicDisplayName = entity.PublicDisplayName,
            PublicAccountNumber = entity.PublicAccountNumber,
            UsesAutomaticPublicDisplayName = entity.UsesAutomaticPublicDisplayName,
            Email = entity.Email,
            HashedPassword = entity.HashedPassword,
            IsActivated = entity.IsActivated,
            IsBlocked = entity.IsBlocked,
            PreferredLanguage = entity.PreferredLanguage,
            PreferredMeasurementSystem = entity.PreferredMeasurementSystem,
            AvatarUrl = entity.AvatarUrl,
            Roles = entity.Roles.ToList(),
            ExternalLogins = entity.ExternalLogins.Select(ToDocument).ToList(),
            LastLoginUtc = entity.LastLoginUtc,
            LastActivityUtc = entity.LastActivityUtc,
            EmailConfirmationTokenHash = entity.EmailConfirmationTokenHash,
            EmailConfirmationTokenExpiresAtUtc = entity.EmailConfirmationTokenExpiresAtUtc,
            EmailConfirmationSentAtUtc = entity.EmailConfirmationSentAtUtc,
            PasswordResetTokenHash = entity.PasswordResetTokenHash,
            PasswordResetTokenExpiresAtUtc = entity.PasswordResetTokenExpiresAtUtc,
            PasswordResetSentAtUtc = entity.PasswordResetSentAtUtc,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static RefreshToken ToDomain(this RefreshTokenDocument document)
    {
        RefreshToken entity = new RefreshToken
        {
            Id = document.Id,
            UserId = document.UserId,
            TokenHash = document.TokenHash,
            ExpiresAtUtc = document.ExpiresAtUtc,
            LastUsedAtUtc = document.LastUsedAtUtc,
            RevokedAtUtc = document.RevokedAtUtc,
            ReplacedByTokenHash = document.ReplacedByTokenHash,
            RevocationReason = document.RevocationReason,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static RefreshTokenDocument ToDocument(this RefreshToken entity)
    {
        return new RefreshTokenDocument
        {
            Id = entity.Id,
            UserId = entity.UserId,
            TokenHash = entity.TokenHash,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            LastUsedAtUtc = entity.LastUsedAtUtc,
            RevokedAtUtc = entity.RevokedAtUtc,
            ReplacedByTokenHash = entity.ReplacedByTokenHash,
            RevocationReason = entity.RevocationReason,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ParkDataEditorAccessToken ToDomain(this ParkDataEditorAccessTokenDocument document)
    {
        ParkDataEditorAccessToken entity = new ParkDataEditorAccessToken
        {
            Id = document.Id,
            UserId = document.UserId,
            Label = document.Label,
            TokenHash = document.TokenHash,
            DisplayPrefix = document.DisplayPrefix,
            ExpiresAtUtc = document.ExpiresAtUtc,
            LastUsedAtUtc = document.LastUsedAtUtc,
            RevokedAtUtc = document.RevokedAtUtc,
            RevokedByUserId = document.RevokedByUserId,
            RevocationReason = document.RevocationReason,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
        };
        return entity;
    }

    public static ParkDataEditorAccessTokenDocument ToDocument(this ParkDataEditorAccessToken entity)
    {
        return new ParkDataEditorAccessTokenDocument
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Label = entity.Label,
            TokenHash = entity.TokenHash,
            DisplayPrefix = entity.DisplayPrefix,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            LastUsedAtUtc = entity.LastUsedAtUtc,
            RevokedAtUtc = entity.RevokedAtUtc,
            RevokedByUserId = entity.RevokedByUserId,
            RevocationReason = entity.RevocationReason,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ExternalLogin ToDomain(this ExternalLoginDocument document)
    {
        return new ExternalLogin
        {
            Provider = document.Provider,
            ProviderUserId = document.ProviderUserId,
            Email = document.Email,
            IsEmailVerified = document.IsEmailVerified,
            DisplayName = document.DisplayName,
            GivenName = document.GivenName,
            FamilyName = document.FamilyName,
            PictureUrl = document.PictureUrl,
            HostedDomain = document.HostedDomain,
            LinkedAtUtc = document.LinkedAtUtc,
            LastLoginAtUtc = document.LastLoginAtUtc,
        };
    }

    public static ExternalLoginDocument ToDocument(this ExternalLogin entity)
    {
        return new ExternalLoginDocument
        {
            Provider = entity.Provider,
            ProviderUserId = entity.ProviderUserId,
            Email = entity.Email,
            IsEmailVerified = entity.IsEmailVerified,
            DisplayName = entity.DisplayName,
            GivenName = entity.GivenName,
            FamilyName = entity.FamilyName,
            PictureUrl = entity.PictureUrl,
            HostedDomain = entity.HostedDomain,
            LinkedAtUtc = entity.LinkedAtUtc,
            LastLoginAtUtc = entity.LastLoginAtUtc,
        };
    }

public static Video ToDomain(this VideoDocument document)
    {
        Video entity = new Video
        {
            Id = document.Id,
            HostingProvider = document.HostingProvider,
            OwnerType = document.OwnerType,
            OwnerId = document.OwnerId,
            Type = document.Type,
            OriginalUrl = document.OriginalUrl,
            CanonicalUrl = document.CanonicalUrl,
            EmbedUrl = document.EmbedUrl,
            ExternalId = document.ExternalId,
            Title = document.Title,
            Description = document.Description,
            CreatorName = document.CreatorName,
            CreatorUrl = document.CreatorUrl,
            ThumbnailUrl = document.ThumbnailUrl,
            ThumbnailImageId = document.ThumbnailImageId,
            Duration = document.DurationSeconds.HasValue ? TimeSpan.FromSeconds(document.DurationSeconds.Value) : null,
            PublishedAtUtc = document.PublishedAtUtc,
            LanguageCodes = document.LanguageCodes,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            TagIds = document.TagIds,
            ExternalMetadata = document.ExternalMetadata.ToDomain(),
            IsPublished = document.IsPublished,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static Video ToVideoDomain(this BsonDocument document)
    {
        Video entity = new Video
        {
            Id = ReadDocumentId(document),
            HostingProvider = ReadEnum(document, "hostingProvider", VideoHostingProvider.Other),
            OwnerType = ReadEnum(document, "ownerType", VideoOwnerType.None),
            OwnerId = ReadOptionalString(document, "ownerId"),
            Type = ReadEnum(document, "type", VideoType.Other),
            OriginalUrl = ReadString(document, "originalUrl"),
            CanonicalUrl = ReadString(document, "canonicalUrl"),
            EmbedUrl = ReadOptionalString(document, "embedUrl"),
            ExternalId = ReadOptionalString(document, "externalId"),
            Title = ReadString(document, "title"),
            Description = ReadOptionalString(document, "description"),
            CreatorName = ReadOptionalString(document, "creatorName"),
            CreatorUrl = ReadOptionalString(document, "creatorUrl"),
            ThumbnailUrl = ReadOptionalString(document, "thumbnailUrl"),
            ThumbnailImageId = ReadOptionalString(document, "thumbnailImageId"),
            Duration = ReadLong(document, "durationSeconds") is long durationSeconds ? TimeSpan.FromSeconds(durationSeconds) : null,
            PublishedAtUtc = ReadDateTime(document, "publishedAtUtc"),
            LanguageCodes = ReadStringList(document, "languageCodes"),
            Titles = ReadLocalizedTexts(document, "titles"),
            Descriptions = ReadLocalizedTexts(document, "descriptions"),
            TagIds = ReadStringList(document, "tagIds"),
            ExternalMetadata = ReadVideoExternalMetadata(document),
            IsPublished = ReadBoolean(document, "isPublished", true),
        };

        entity.CreatedAtUtc = ReadDateTime(document, "createdAt") ?? DateTime.UtcNow;
        entity.UpdatedAtUtc = ReadDateTime(document, "updatedAt") ?? entity.CreatedAtUtc;
        return entity;
    }

    public static VideoDocument ToDocument(this Video entity)
    {
        return new VideoDocument
        {
            Id = entity.Id,
            HostingProvider = entity.HostingProvider,
            OwnerType = entity.OwnerType,
            OwnerId = entity.OwnerId,
            Type = entity.Type,
            OriginalUrl = entity.OriginalUrl,
            CanonicalUrl = entity.CanonicalUrl,
            EmbedUrl = entity.EmbedUrl,
            ExternalId = entity.ExternalId,
            Title = entity.Title,
            Description = entity.Description,
            CreatorName = entity.CreatorName,
            CreatorUrl = entity.CreatorUrl,
            ThumbnailUrl = entity.ThumbnailUrl,
            ThumbnailImageId = entity.ThumbnailImageId,
            DurationSeconds = entity.Duration.HasValue ? checked((long)entity.Duration.Value.TotalSeconds) : null,
            PublishedAtUtc = entity.PublishedAtUtc,
            LanguageCodes = entity.LanguageCodes,
            Titles = CommonMongoMappers.ToDocuments(entity.Titles),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            TagIds = entity.TagIds,
            ExternalMetadata = entity.ExternalMetadata.ToDocument(),
            IsPublished = entity.IsPublished,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static VideoExternalMetadata ToDomain(this VideoExternalMetadataDocument document)
    {
        return new VideoExternalMetadata
        {
            Source = document.Source,
            FetchedAtUtc = document.FetchedAtUtc,
            ProviderTitle = document.ProviderTitle,
            ProviderDescription = document.ProviderDescription,
            ProviderChannelId = document.ProviderChannelId,
            ProviderChannelUrl = document.ProviderChannelUrl,
            ProviderViewCount = document.ProviderViewCount,
        };
    }

    public static VideoExternalMetadataDocument ToDocument(this VideoExternalMetadata entity)
    {
        return new VideoExternalMetadataDocument
        {
            Source = entity.Source,
            FetchedAtUtc = entity.FetchedAtUtc,
            ProviderTitle = entity.ProviderTitle,
            ProviderDescription = entity.ProviderDescription,
            ProviderChannelId = entity.ProviderChannelId,
            ProviderChannelUrl = entity.ProviderChannelUrl,
            ProviderViewCount = entity.ProviderViewCount,
        };
    }

    public static VideoTag ToDomain(this VideoTagDocument document)
    {
        VideoTag entity = new VideoTag
        {
            Id = document.Id,
            Slug = document.Slug,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsActive = document.IsActive,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static VideoTag ToVideoTagDomain(this BsonDocument document)
    {
        VideoTag entity = new VideoTag
        {
            Id = ReadDocumentId(document),
            Slug = ReadString(document, "slug"),
            Labels = ReadLocalizedTexts(document, "labels"),
            Descriptions = ReadLocalizedTexts(document, "descriptions"),
            IsActive = ReadBoolean(document, "isActive", true),
        };

        entity.CreatedAtUtc = ReadDateTime(document, "createdAt") ?? DateTime.UtcNow;
        entity.UpdatedAtUtc = ReadDateTime(document, "updatedAt") ?? entity.CreatedAtUtc;
        return entity;
    }

    public static VideoTagDocument ToDocument(this VideoTag entity)
    {
        return new VideoTagDocument
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Labels = CommonMongoMappers.ToDocuments(entity.Labels),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    private static VideoExternalMetadata ReadVideoExternalMetadata(BsonDocument document)
    {
        BsonDocument metadataDocument = ReadDocument(document, "externalMetadata");
        return new VideoExternalMetadata
        {
            Source = ReadOptionalString(metadataDocument, "source"),
            FetchedAtUtc = ReadDateTime(metadataDocument, "fetchedAtUtc"),
            ProviderTitle = ReadOptionalString(metadataDocument, "providerTitle"),
            ProviderDescription = ReadOptionalString(metadataDocument, "providerDescription"),
            ProviderChannelId = ReadOptionalString(metadataDocument, "providerChannelId"),
            ProviderChannelUrl = ReadOptionalString(metadataDocument, "providerChannelUrl"),
            ProviderViewCount = ReadLong(metadataDocument, "providerViewCount"),
        };
    }

    private static string ReadDocumentId(BsonDocument document)
    {
        if (!document.TryGetValue("_id", out BsonValue? idValue) || IsNullLike(idValue))
        {
            return string.Empty;
        }

        return idValue switch
        {
            { IsString: true } => idValue.AsString,
            { IsObjectId: true } => idValue.AsObjectId.ToString(),
            _ => idValue.ToString() ?? string.Empty,
        };
    }

    private static string ReadString(BsonDocument document, string fieldName)
    {
        return ReadOptionalString(document, fieldName) ?? string.Empty;
    }

    private static string? ReadOptionalString(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || IsNullLike(value))
        {
            return null;
        }

        if (value.IsString)
        {
            string normalizedValue = value.AsString.Trim();
            return normalizedValue.Length > 0 ? normalizedValue : null;
        }

        return value.ToString();
    }

    private static bool ReadBoolean(BsonDocument document, string fieldName, bool defaultValue)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || IsNullLike(value))
        {
            return defaultValue;
        }

        if (value.IsBoolean)
        {
            return value.AsBoolean;
        }

        if (value.IsString && bool.TryParse(value.AsString, out bool parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static long? ReadLong(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || IsNullLike(value))
        {
            return null;
        }

        if (value.IsInt64)
        {
            return value.AsInt64;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        if (value.IsDouble)
        {
            return checked((long)value.AsDouble);
        }

        if (value.IsString && long.TryParse(value.AsString, out long parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTime? ReadDateTime(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || IsNullLike(value))
        {
            return null;
        }

        if (value.IsValidDateTime)
        {
            return value.ToUniversalTime();
        }

        if (value.IsString && DateTime.TryParse(value.AsString, out DateTime parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc) : parsed.ToUniversalTime();
        }

        return null;
    }

    private static TEnum ReadEnum<TEnum>(BsonDocument document, string fieldName, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        string? rawValue = ReadOptionalString(document, fieldName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (Enum.TryParse(rawValue, true, out TEnum parsed))
        {
            return parsed;
        }

        string normalizedValue = NormalizeEnumToken(rawValue);
        foreach (string enumName in Enum.GetNames<TEnum>())
        {
            if (string.Equals(NormalizeEnumToken(enumName), normalizedValue, StringComparison.OrdinalIgnoreCase))
            {
                return Enum.Parse<TEnum>(enumName);
            }
        }

        return defaultValue;
    }

    private static string NormalizeEnumToken(string value)
    {
        return value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static List<string> ReadStringList(BsonDocument document, string fieldName)
    {
        List<string> values = new List<string>();
        if (!document.TryGetValue(fieldName, out BsonValue? value) || !value.IsBsonArray)
        {
            return values;
        }

        foreach (BsonValue item in value.AsBsonArray)
        {
            if (item.IsString && !string.IsNullOrWhiteSpace(item.AsString))
            {
                values.Add(item.AsString.Trim());
            }
        }

        return values.Distinct(StringComparer.Ordinal).ToList();
    }

    private static List<LocalizedText> ReadLocalizedTexts(BsonDocument document, string fieldName)
    {
        List<LocalizedText> values = new List<LocalizedText>();
        if (!document.TryGetValue(fieldName, out BsonValue? value) || !value.IsBsonArray)
        {
            return values;
        }

        foreach (BsonValue item in value.AsBsonArray)
        {
            if (!item.IsBsonDocument)
            {
                continue;
            }

            BsonDocument localizedDocument = item.AsBsonDocument;
            string languageCode = ReadString(localizedDocument, "languageCode");
            string? localizedValue = ReadOptionalString(localizedDocument, "value");
            if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(localizedValue))
            {
                values.Add(new LocalizedText(languageCode.Trim().ToLowerInvariant(), localizedValue.Trim()));
            }
        }

        return values;
    }

    private static BsonDocument ReadDocument(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || !value.IsBsonDocument)
        {
            return new BsonDocument();
        }

        return value.AsBsonDocument;
    }

    private static bool IsNullLike(BsonValue value)
    {
        return value.IsBsonNull || value.BsonType == BsonType.Undefined;
    }

private const string WeatherDateFormat = "yyyy-MM-dd";

    public static ParkWeatherDailySnapshotDocument ToDocument(this ParkWeatherDailySnapshot snapshot)
    {
        DateTime now = DateTime.UtcNow;
        return new ParkWeatherDailySnapshotDocument
        {
            Id = string.IsNullOrWhiteSpace(snapshot.Id) ? Guid.NewGuid().ToString() : snapshot.Id,
            CreatedAt = now,
            UpdatedAt = now,
            ParkId = snapshot.ParkId,
            LocalDate = FormatDate(snapshot.LocalDate),
            DataKind = snapshot.DataKind,
            SourceProvider = snapshot.SourceProvider,
            FetchedAtUtc = snapshot.FetchedAtUtc,
            ProviderGeneratedAtUtc = snapshot.ProviderGeneratedAtUtc,
            TimeZone = snapshot.TimeZone,
            UtcOffsetSeconds = snapshot.UtcOffsetSeconds,
            Latitude = snapshot.Latitude,
            Longitude = snapshot.Longitude,
            WeatherCode = snapshot.WeatherCode,
            TemperatureMinCelsius = snapshot.TemperatureMinCelsius,
            TemperatureMaxCelsius = snapshot.TemperatureMaxCelsius,
            ApparentTemperatureMinCelsius = snapshot.ApparentTemperatureMinCelsius,
            ApparentTemperatureMaxCelsius = snapshot.ApparentTemperatureMaxCelsius,
            PrecipitationProbabilityMaxPercent = snapshot.PrecipitationProbabilityMaxPercent,
            PrecipitationSumMillimeters = snapshot.PrecipitationSumMillimeters,
            WindSpeedMaxKilometersPerHour = snapshot.WindSpeedMaxKilometersPerHour,
            WindGustsMaxKilometersPerHour = snapshot.WindGustsMaxKilometersPerHour,
        };
    }

    public static ParkWeatherDailySnapshot ToDomain(this ParkWeatherDailySnapshotDocument document)
    {
        return new ParkWeatherDailySnapshot
        {
            Id = document.Id,
            ParkId = document.ParkId,
            LocalDate = ParseDate(document.LocalDate),
            DataKind = document.DataKind,
            SourceProvider = document.SourceProvider,
            FetchedAtUtc = document.FetchedAtUtc,
            ProviderGeneratedAtUtc = document.ProviderGeneratedAtUtc,
            TimeZone = document.TimeZone,
            UtcOffsetSeconds = document.UtcOffsetSeconds,
            Latitude = document.Latitude,
            Longitude = document.Longitude,
            WeatherCode = document.WeatherCode,
            TemperatureMinCelsius = document.TemperatureMinCelsius,
            TemperatureMaxCelsius = document.TemperatureMaxCelsius,
            ApparentTemperatureMinCelsius = document.ApparentTemperatureMinCelsius,
            ApparentTemperatureMaxCelsius = document.ApparentTemperatureMaxCelsius,
            PrecipitationProbabilityMaxPercent = document.PrecipitationProbabilityMaxPercent,
            PrecipitationSumMillimeters = document.PrecipitationSumMillimeters,
            WindSpeedMaxKilometersPerHour = document.WindSpeedMaxKilometersPerHour,
            WindGustsMaxKilometersPerHour = document.WindGustsMaxKilometersPerHour,
        };
    }

    public static ParkWeatherRunDocument ToDocument(this ParkWeatherRun run)
    {
        DateTime now = DateTime.UtcNow;
        return new ParkWeatherRunDocument
        {
            Id = string.IsNullOrWhiteSpace(run.Id) ? Guid.NewGuid().ToString() : run.Id,
            CreatedAt = now,
            UpdatedAt = now,
            Trigger = run.Trigger,
            Scope = run.Scope,
            Status = run.Status,
            SourceRunId = run.SourceRunId,
            TargetParkId = run.TargetParkId,
            CancelsAutomaticRunLocalDate = run.CancelsAutomaticRunLocalDate.HasValue ? FormatDate(run.CancelsAutomaticRunLocalDate.Value) : null,
            RequestedAtUtc = run.RequestedAtUtc,
            StartedAtUtc = run.StartedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            TotalParkCount = run.TotalParkCount,
            SucceededParkCount = run.SucceededParkCount,
            FailedParkCount = run.FailedParkCount,
            SkippedParkCount = run.SkippedParkCount,
            WarningParkCount = run.WarningParkCount,
            Message = run.Message,
        };
    }

    public static ParkWeatherRun ToDomain(this ParkWeatherRunDocument document)
    {
        return new ParkWeatherRun
        {
            Id = document.Id,
            Trigger = document.Trigger,
            Scope = document.Scope,
            Status = document.Status,
            SourceRunId = document.SourceRunId,
            TargetParkId = document.TargetParkId,
            CancelsAutomaticRunLocalDate = string.IsNullOrWhiteSpace(document.CancelsAutomaticRunLocalDate) ? null : ParseDate(document.CancelsAutomaticRunLocalDate),
            RequestedAtUtc = document.RequestedAtUtc,
            StartedAtUtc = document.StartedAtUtc,
            CompletedAtUtc = document.CompletedAtUtc,
            TotalParkCount = document.TotalParkCount,
            SucceededParkCount = document.SucceededParkCount,
            FailedParkCount = document.FailedParkCount,
            SkippedParkCount = document.SkippedParkCount,
            WarningParkCount = document.WarningParkCount,
            Message = document.Message,
        };
    }

    public static ParkWeatherRunItemDocument ToDocument(this ParkWeatherRunItem item)
    {
        DateTime now = DateTime.UtcNow;
        return new ParkWeatherRunItemDocument
        {
            Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString() : item.Id,
            CreatedAt = now,
            UpdatedAt = now,
            RunId = item.RunId,
            ParkId = item.ParkId,
            ParkName = item.ParkName,
            Status = item.Status,
            AttemptCount = item.AttemptCount,
            StartedAtUtc = item.StartedAtUtc,
            CompletedAtUtc = item.CompletedAtUtc,
            ForecastDayCount = item.ForecastDayCount,
            ObservationDayCount = item.ObservationDayCount,
            WarningMessage = item.WarningMessage,
            ErrorCode = item.ErrorCode,
            ErrorMessage = item.ErrorMessage,
        };
    }

    public static ParkWeatherRunItem ToDomain(this ParkWeatherRunItemDocument document)
    {
        return new ParkWeatherRunItem
        {
            Id = document.Id,
            RunId = document.RunId,
            ParkId = document.ParkId,
            ParkName = document.ParkName,
            Status = document.Status,
            AttemptCount = document.AttemptCount,
            StartedAtUtc = document.StartedAtUtc,
            CompletedAtUtc = document.CompletedAtUtc,
            ForecastDayCount = document.ForecastDayCount,
            ObservationDayCount = document.ObservationDayCount,
            WarningMessage = document.WarningMessage,
            ErrorCode = document.ErrorCode,
            ErrorMessage = document.ErrorMessage,
        };
    }

    internal static string FormatDate(DateOnly date)
    {
        return date.ToString(WeatherDateFormat, CultureInfo.InvariantCulture);
    }

    internal static DateOnly ParseDate(string date)
    {
        return DateOnly.ParseExact(date, WeatherDateFormat, CultureInfo.InvariantCulture);
    }
}
