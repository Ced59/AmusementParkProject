using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Models;
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

        PassportProfileShareInput? passportProfile = null;
        if (request.PassportProfile is not null
            && !TryToApplication(request.PassportProfile, out passportProfile))
        {
            return false;
        }

        command = new PublishSharePublicationCommand(
            ownerUserId,
            publicationType,
            string.IsNullOrWhiteSpace(request.SourceId) ? null : request.SourceId.Trim(),
            request.ApprovedSourceVersion,
            request.ApprovedPolicySchemaVersion,
            datePrecision,
            includedFields.Distinct().ToArray(),
            request.ApprovalToken.Trim(),
            request.VisitRecap?.ToApplication(),
            request.YearRecap?.ToApplication(),
            passportProfile);
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

        PassportProfileShareInput? passportProfile = null;
        if (request.PassportProfile is not null
            && !TryToApplication(request.PassportProfile, out passportProfile))
        {
            return false;
        }

        query = new PreviewSharePublicationQuery(
            ownerUserId,
            publicationType,
            string.IsNullOrWhiteSpace(request.SourceId) ? null : request.SourceId.Trim(),
            datePrecision,
            includedFields.Distinct().ToArray(),
            request.VisitRecap?.ToApplication(),
            request.YearRecap?.ToApplication(),
            passportProfile);
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
            VisitRecap = value.VisitRecap?.ToHttp(),
            YearRecap = value.YearRecap?.ToHttp(),
            PassportProfile = value.PassportProfile?.ToHttp(),
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
            Visibility = value.Visibility?.ToString(),
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

    public static VisitRecapSharePreviewDto ToHttp(this VisitRecapSharePreviewResult value)
    {
        return new VisitRecapSharePreviewDto
        {
            ParkId = value.ParkId,
            ParkName = value.ParkName,
            Date = value.Date is null
                ? null
                : new VisitRecapShareDateDto
                {
                    Year = value.Date.Year,
                    Month = value.Date.Month,
                    Day = value.Date.Day,
                    Precision = value.Date.Precision.ToString(),
                    IsApproximate = value.Date.IsApproximate,
                },
            DistinctItemCount = value.DistinctItemCount,
            TotalRideCount = value.TotalRideCount,
            Categories = value.Categories.ToList(),
            ParkRating = value.ParkRating,
            TopRatedItem = value.TopRatedItem?.ToHttp(),
            MostRepeatedItem = value.MostRepeatedItem?.ToHttp(),
            Items = value.Items.Select(static item => item.ToHttp()).ToList(),
            PublicCaption = value.PublicCaption,
            HasHiddenDate = value.HasHiddenDate,
            HasIncompleteRatings = value.HasIncompleteRatings,
            HasIncompleteItems = value.HasIncompleteItems,
        };
    }

    public static VisitRecapShareCandidatesDto ToHttp(
        this VisitRecapShareCandidatesResult value)
    {
        return new VisitRecapShareCandidatesDto
        {
            Items = value.Items.Select(static item => item.ToHttp()).ToList(),
            TotalEligibleItemCount = value.TotalEligibleItemCount,
            IsTruncated = value.IsTruncated,
            SavedSelectedParkItemIds = value.SavedSelectedParkItemIds?.ToList(),
            SavedPublicCaption = value.SavedPublicCaption,
            HasSavedSnapshot = value.HasSavedSnapshot,
        };
    }

    public static SharedVisitRecapContentDto ToPublicHttp(this VisitRecapSharePreviewResult value)
    {
        return new SharedVisitRecapContentDto
        {
            ParkId = value.ParkId,
            ParkName = value.ParkName,
            Date = value.Date is null
                ? null
                : new VisitRecapShareDateDto
                {
                    Year = value.Date.Year,
                    Month = value.Date.Month,
                    Day = value.Date.Day,
                    Precision = value.Date.Precision.ToString(),
                    IsApproximate = value.Date.IsApproximate,
                },
            DistinctItemCount = value.DistinctItemCount,
            TotalRideCount = value.TotalRideCount,
            Categories = value.Categories.ToList(),
            ParkRating = value.ParkRating,
            TopRatedItem = value.TopRatedItem?.ToHttp(),
            MostRepeatedItem = value.MostRepeatedItem?.ToHttp(),
            Items = value.Items.Select(static item => item.ToPublicHttp()).ToList(),
            PublicCaption = value.PublicCaption,
            HasHiddenDate = value.HasHiddenDate,
            HasIncompleteRatings = value.HasIncompleteRatings,
            HasIncompleteItems = value.HasIncompleteItems,
        };
    }

    private static VisitRecapShareInput ToApplication(this VisitRecapShareInputDto value)
    {
        return new VisitRecapShareInput(value.SelectedParkItemIds, value.PublicCaption);
    }

    private static YearRecapShareInput ToApplication(this YearRecapShareInputDto value)
    {
        return new YearRecapShareInput(value.PublicCaption);
    }

    private static bool TryToApplication(
        PassportProfileShareInputDto value,
        out PassportProfileShareInput? input)
    {
        input = null;
        if (!TryParseDefined(value.Visibility, out ShareVisibility visibility)
            || visibility is not ShareVisibility.Unlisted and not ShareVisibility.Public)
        {
            return false;
        }

        input = new PassportProfileShareInput(
            value.SelectedYears,
            value.SelectedParkIds,
            value.SelectedRatingKeys,
            value.PublicCaption,
            visibility,
            value.AllowsComparisons);
        return true;
    }

    public static PassportProfileShareSelectionDto ToHttp(
        this PassportProfileShareSelectionResult value)
    {
        return new PassportProfileShareSelectionDto
        {
            Years = value.Years.Select(static item => new PassportProfileShareYearCandidateDto
            {
                Year = item.Year,
                VisitCount = item.VisitCount,
            }).ToList(),
            Parks = value.Parks.Select(static item => new PassportProfileShareParkCandidateDto
            {
                ParkId = item.ParkId,
                Name = item.Name,
                CountryCode = item.CountryCode,
                VisitCount = item.VisitCount,
            }).ToList(),
            Ratings = value.Ratings.Select(static item => new PassportProfileShareRatingCandidateDto
            {
                SelectionKey = item.SelectionKey,
                ParkId = item.ParkId,
                Name = item.Name,
                ParkName = item.ParkName,
                Rating = item.Rating,
            }).ToList(),
            MaximumSelectedYears = value.MaximumSelectedYears,
            MaximumSelectedParks = value.MaximumSelectedParks,
            SavedSelectedYears = value.SavedSelectedYears?.ToList(),
            SavedSelectedParkIds = value.SavedSelectedParkIds?.ToList(),
            SavedSelectedRatingKeys = value.SavedSelectedRatingKeys?.ToList(),
            SavedPublicCaption = value.SavedPublicCaption,
            SavedVisibility = value.SavedVisibility.ToString(),
            SavedAllowsComparisons = value.SavedAllowsComparisons,
            HasSavedSnapshot = value.HasSavedSnapshot,
        };
    }

    public static PassportProfileSharePreviewDto ToHttp(
        this PassportProfileSharePreviewResult value)
    {
        return new PassportProfileSharePreviewDto
        {
            DisplayName = value.DisplayName,
            AvatarUrl = value.AvatarUrl,
            PublicCaption = value.PublicCaption,
            Visibility = value.Visibility.ToString(),
            AllowsComparisons = value.AllowsComparisons,
            ParkCount = value.ParkCount,
            VisitCount = value.VisitCount,
            TotalRideCount = value.TotalRideCount,
            DistinctItemCount = value.DistinctItemCount,
            VisitRatings = value.VisitRatings?.ToHttp(),
            RideRatings = value.RideRatings?.ToHttp(),
            Countries = value.Countries.Select(static item => new PassportProfileShareCountryDto
            {
                CountryCode = item.CountryCode,
                ParkCount = item.ParkCount,
                VisitCount = item.VisitCount,
            }).ToList(),
            Years = value.Years.Select(static item => new PassportProfileShareYearDto
            {
                Year = item.Year,
                VisitCount = item.VisitCount,
                ParkCount = item.ParkCount,
                CompletedRideCount = item.CompletedRideCount,
            }).ToList(),
            Parks = value.Parks.Select(static item => new PassportProfileShareParkDto
            {
                Name = item.Name,
                CountryCode = item.CountryCode,
                VisitCount = item.VisitCount,
                FirstVisitYear = item.FirstVisitYear,
                LastVisitYear = item.LastVisitYear,
                CompletedRideCount = item.CompletedRideCount,
                VisitRatings = item.VisitRatings?.ToHttp(),
            }).ToList(),
            PersonalRanking = value.PersonalRanking.Select(static item => new PassportProfileShareRatingDto
            {
                TargetType = item.TargetType,
                Name = item.Name,
                ParkName = item.ParkName,
                Category = item.Category,
                Rating = item.Rating,
            }).ToList(),
            MissedItems = value.MissedItems.Select(static item => new PassportProfileShareMissedItemDto
            {
                Name = item.Name,
                Status = item.Status,
                OccurrenceCount = item.OccurrenceCount,
            }).ToList(),
            HasIncompleteCatalog = value.HasIncompleteCatalog,
            CalculationVersion = value.CalculationVersion,
            IsEmpty = value.IsEmpty,
        };
    }

    private static PassportProfileShareRatingSummaryDto ToHttp(
        this PassportProfileShareRatingSummaryResult value)
    {
        return new PassportProfileShareRatingSummaryDto
        {
            RatedCount = value.RatedCount,
            EligibleCount = value.EligibleCount,
            Average = value.Average,
        };
    }

    public static YearRecapSharePreviewDto ToHttp(this YearRecapSharePreviewResult value)
    {
        return new YearRecapSharePreviewDto
        {
            Year = value.Year,
            ParkCount = value.ParkCount,
            VisitCount = value.VisitCount,
            ApproximateVisitCount = value.ApproximateVisitCount,
            ApproximateVisitRate = value.ApproximateVisitRate,
            TotalRideCount = value.TotalRideCount,
            DistinctItemCount = value.DistinctItemCount,
            MissedItemCount = value.MissedItemCount,
            Categories = value.Categories.ToList(),
            ParkRatings = value.ParkRatings?.ToHttp(),
            RideRatings = value.RideRatings?.ToHttp(),
            MostVisitedParks = value.MostVisitedParks.Select(static item => item.ToHttp()).ToList(),
            MostRepeatedItem = value.MostRepeatedItem?.ToHttp(),
            TopRatedItem = value.TopRatedItem?.ToHttp(),
            RatingEvolution = value.RatingEvolution?.ToHttp(),
            NowClosedItems = value.NowClosedItems.Select(static item => item.ToHttp()).ToList(),
            PublicCaption = value.PublicCaption,
            HasIncompleteCatalog = value.HasIncompleteCatalog,
            CalculationVersion = value.CalculationVersion,
            IsEmpty = value.IsEmpty,
        };
    }

    private static YearRecapShareRatingSummaryDto ToHttp(
        this YearRecapShareRatingSummaryResult value)
    {
        return new YearRecapShareRatingSummaryDto
        {
            RatedCount = value.RatedCount,
            EligibleCount = value.EligibleCount,
            Average = value.Average,
        };
    }

    private static YearRecapShareParkDto ToHttp(this YearRecapShareParkResult value)
    {
        return new YearRecapShareParkDto
        {
            Name = value.Name,
            VisitCount = value.VisitCount,
            CompletedRideCount = value.CompletedRideCount,
        };
    }

    private static YearRecapShareHighlightDto ToHttp(this YearRecapShareHighlightResult value)
    {
        return new YearRecapShareHighlightDto
        {
            Name = value.Name,
            RideCount = value.RideCount,
            RatingCount = value.RatingCount,
            AverageRating = value.AverageRating,
            IsNowClosed = value.IsNowClosed,
        };
    }

    private static YearRecapShareTrendDto ToHttp(this YearRecapShareTrendResult value)
    {
        return new YearRecapShareTrendDto
        {
            Name = value.Name,
            Kind = value.Kind,
            FirstWindowRatingCount = value.FirstWindowRatingCount,
            LastWindowRatingCount = value.LastWindowRatingCount,
            FirstWindowAverage = value.FirstWindowAverage,
            LastWindowAverage = value.LastWindowAverage,
            Delta = value.Delta,
        };
    }

    private static VisitRecapShareHighlightDto ToHttp(
        this VisitRecapShareHighlightResult value)
    {
        return new VisitRecapShareHighlightDto
        {
            Name = value.Name,
            RideCount = value.RideCount,
            Rating = value.Rating,
        };
    }

    private static VisitRecapShareItemDto ToHttp(this VisitRecapShareItemResult value)
    {
        return new VisitRecapShareItemDto
        {
            ParkItemId = value.ParkItemId,
            Name = value.Name,
            Category = value.Category,
            RideCount = value.RideCount,
            AverageRating = value.AverageRating,
            IsMissed = value.IsMissed,
        };
    }

    private static SharedVisitRecapItemDto ToPublicHttp(this VisitRecapShareItemResult value)
    {
        return new SharedVisitRecapItemDto
        {
            Name = value.Name,
            Category = value.Category,
            RideCount = value.RideCount,
            AverageRating = value.AverageRating,
            IsMissed = value.IsMissed,
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
