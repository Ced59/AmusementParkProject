using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialShare;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Services.Passport;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class MongoAccountCommunityExportSourceTests
{
    [Fact]
    public void Mappers_ReplaceTechnicalRelationsWithReadableNamesAndLocalReferences()
    {
        DateTime nowUtc = new DateTime(2026, 10, 3, 19, 0, 0, DateTimeKind.Utc);
        CommentDocument comment = new CommentDocument
        {
            Id = "internal-comment-id",
            TargetType = CommentTargetType.ParkItem,
            TargetId = "internal-item-id",
            ParkId = "internal-park-id",
            AuthorUserId = "internal-user-id",
            Bodies = new List<LocalizedTextDocument>
            {
                new LocalizedTextDocument { LanguageCode = "fr", Value = "Très bon souvenir" },
            },
            ImageIds = new List<string> { "internal-image-id" },
            Revision = 2,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
        };
        AccountCommentExportData exportedComment =
            MongoAccountCommunityExportSource.MapComment(
                comment,
                "comment-0001",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["internal-image-id"] = "image-0001",
                },
                new Dictionary<string, ParkDocument>(StringComparer.Ordinal)
                {
                    ["internal-park-id"] = new ParkDocument
                    {
                        Id = "internal-park-id",
                        Name = "Europa Park",
                    },
                },
                new Dictionary<string, ParkItemDocument>(StringComparer.Ordinal)
                {
                    ["internal-item-id"] = new ParkItemDocument
                    {
                        Id = "internal-item-id",
                        ParkId = "internal-park-id",
                        Name = "Silver Star",
                    },
                });
        ImageDocument image = new ImageDocument
        {
            Id = "internal-image-id",
            DraftOwnerId = "internal-user-id",
            OwnerId = "internal-comment-id",
            OwnerType = ImageOwnerType.Comment,
            OriginalFileName = "souvenir.jpg",
            ContentType = "image/jpeg",
            Width = 1200,
            Height = 800,
            SizeInBytes = 42_000,
            GeoLocation = new GeoPointDocument { Latitude = 48.2, Longitude = 7.7 },
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
        };
        AccountImageExportData exportedImage =
            MongoAccountCommunityExportSource.MapImage(image, "image-0001");
        HistoricalExistenceReportDocument report = new HistoricalExistenceReportDocument
        {
            Id = "internal-report-id",
            OwnerUserId = "internal-user-id",
            VisitId = "internal-visit-id",
            ParkId = "internal-park-id",
            ParkName = "Europa Park",
            VisitDate = new VisitDateDocument { Year = 2002 },
            ClaimedName = "Silver Star",
            SubmittedAtUtc = nowUtc,
            Revision = 1,
        };
        AccountHistoricalReportExportData exportedReport =
            MongoAccountCommunityExportSource.MapHistoricalReport(report);
        SocialShareEventDocument socialShare = new SocialShareEventDocument
        {
            Id = "internal-share-id",
            UserId = "internal-user-id",
            TargetId = "internal-target-id",
            TargetType = "Park",
            TargetTitle = "Europa Park",
            Url = "/parks/internal-target-id",
            Channel = "CopyLink",
            OccurredAtUtc = nowUtc,
        };
        AccountSocialShareExportData exportedSocialShare =
            MongoAccountCommunityExportSource.MapSocialShare(socialShare);

        Assert.Equal("Silver Star", exportedComment.TargetName);
        Assert.Equal("Europa Park", exportedComment.ParkName);
        Assert.Equal("image-0001", Assert.Single(exportedComment.ImageReferences));
        Assert.Equal("souvenir.jpg", exportedImage.OriginalFileName);
        Assert.Equal(48.2, exportedImage.Latitude);
        Assert.Equal("Europa Park", exportedReport.ParkName);
        Assert.Equal("Europa Park", exportedSocialShare.TargetTitle);
        string serialized = JsonSerializer.Serialize(new
        {
            exportedComment,
            exportedImage,
            exportedReport,
            exportedSocialShare,
        });
        Assert.DoesNotContain("internal-user-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-comment-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-item-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-park-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-image-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-report-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-visit-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-share-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-target-id", serialized, StringComparison.Ordinal);
    }
}
