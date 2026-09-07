using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Contracts.Sharing;

namespace AmusementPark.WebAPI.Mappers;

public static class SharingHttpMappers
{
    public static bool TryToApplication(
        this PublishSharePublicationRequestDto request,
        string ownerUserId,
        out PublishSharePublicationCommand? command)
    {
        command = null;
        if (!TryParseDefined(request.PublicationType, out SharePublicationType publicationType)
            || !TryParseDefined(request.ApprovedDatePrecision, out ShareDatePrecision datePrecision)
            || request.ApprovedSourceVersion < 0
            || string.IsNullOrWhiteSpace(request.ApprovalToken))
        {
            return false;
        }

        List<ShareContentField> includedFields = new List<ShareContentField>();
        foreach (string fieldValue in request.ApprovedIncludedFields ?? new List<string>())
        {
            if (!TryParseDefined(fieldValue, out ShareContentField field))
            {
                return false;
            }

            includedFields.Add(field);
        }

        command = new PublishSharePublicationCommand(
            ownerUserId,
            publicationType,
            string.IsNullOrWhiteSpace(request.SourceId) ? null : request.SourceId.Trim(),
            request.ApprovedSourceVersion,
            request.ApprovedPolicySchemaVersion,
            datePrecision,
            includedFields.Distinct().ToArray(),
            request.ApprovalToken.Trim());
        return true;
    }

    public static bool TryToApplication(
        this SharePublicationPreviewRequestDto request,
        string ownerUserId,
        out PreviewSharePublicationQuery? query)
    {
        query = null;
        if (!TryParseDefined(request.PublicationType, out SharePublicationType publicationType)
            || !TryParseDefined(request.DatePrecision, out ShareDatePrecision datePrecision))
        {
            return false;
        }

        List<ShareContentField> includedFields = new List<ShareContentField>();
        foreach (string fieldValue in request.IncludedFields ?? new List<string>())
        {
            if (!TryParseDefined(fieldValue, out ShareContentField field))
            {
                return false;
            }

            includedFields.Add(field);
        }

        query = new PreviewSharePublicationQuery(
            ownerUserId,
            publicationType,
            string.IsNullOrWhiteSpace(request.SourceId) ? null : request.SourceId.Trim(),
            datePrecision,
            includedFields.Distinct().ToArray());
        return true;
    }

    public static SharePublicationPreviewDto ToHttp(this SharePublicationPreviewResult value)
    {
        return new SharePublicationPreviewDto
        {
            PublicationType = value.PublicationType.ToString(),
            SourceVersion = value.SourceVersion,
            ApprovalToken = value.ApprovalToken,
            ContentPolicy = new ShareContentPolicyPreviewDto
            {
                SchemaVersion = value.PolicySchemaVersion,
                DatePrecision = value.DatePrecision.ToString(),
                IncludedFields = value.IncludedFields
                    .Select(static field => field.ToString())
                    .ToList(),
            },
            PersonalRanking = value.PersonalRanking?.ToHttp(),
        };
    }

    public static SharePublicationSettingsDto ToSharingHttp(this SharePublicationSettingsResult value)
    {
        return new SharePublicationSettingsDto
        {
            IsPublic = value.IsPublic,
            ShareId = value.ShareId,
            PublishedAtUtc = value.PublishedAtUtc,
            PolicySchemaVersion = value.PolicySchemaVersion,
            DatePrecision = value.DatePrecision?.ToString(),
            IncludedFields = value.IncludedFields
                .Select(static field => field.ToString())
                .ToList(),
        };
    }

    private static PersonalRankingSharePreviewDto ToHttp(
        this PersonalRankingSharePreviewResult value)
    {
        return new PersonalRankingSharePreviewDto
        {
            DisplayName = value.DisplayName,
            AvatarUrl = value.AvatarUrl,
            Statistics = value.Statistics?.ToHttp(),
            Ratings = value.Ratings.Select(static rating => rating.ToHttp()).ToList(),
            IsTruncated = value.IsTruncated,
        };
    }

    private static PersonalRankingShareStatisticsDto ToHttp(
        this PersonalRankingShareStatisticsResult value)
    {
        return new PersonalRankingShareStatisticsDto
        {
            TotalRatings = value.TotalRatings,
            AverageRating = value.AverageRating,
            HighestRating = value.HighestRating,
            LowestRating = value.LowestRating,
            ByPark = value.ByPark.Select(static bucket => bucket.ToHttp()).ToList(),
            ByTargetType = value.ByTargetType.Select(static bucket => bucket.ToHttp()).ToList(),
            ByParkItemCategory = value.ByParkItemCategory
                .Select(static bucket => bucket.ToHttp())
                .ToList(),
        };
    }

    private static PersonalRankingShareStatBucketDto ToHttp(
        this PersonalRankingShareStatBucketResult value)
    {
        return new PersonalRankingShareStatBucketDto
        {
            Key = value.Key,
            Label = value.Label,
            Count = value.Count,
            AverageRating = value.AverageRating,
        };
    }

    private static PersonalRankingSharePreviewItemDto ToHttp(
        this PersonalRankingSharePreviewItemResult value)
    {
        return new PersonalRankingSharePreviewItemDto
        {
            TargetType = value.TargetType.ToString(),
            TargetName = value.TargetName,
            ParkName = value.ParkName,
            ParkItemCategory = value.ParkItemCategory?.ToString(),
            ParkItemType = value.ParkItemType?.ToString(),
            Rating = value.Rating,
        };
    }

    private static bool TryParseDefined<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        parsed = default;
        string? normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || !Enum.TryParse(normalized, ignoreCase: true, out TEnum candidate))
        {
            return false;
        }

        string? canonicalName = Enum.GetName(candidate);
        if (canonicalName is null
            || !string.Equals(canonicalName, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        parsed = candidate;
        return true;
    }
}
