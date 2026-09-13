using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Contracts.Sharing;

namespace AmusementPark.WebAPI.Mappers;

public static class ProfileComparisonHttpMapper
{
    public static SharedProfileComparisonDto ToHttp(this SharedProfileComparisonResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ProfileComparisonCalculation calculation = result.Calculation;
        return new SharedProfileComparisonDto
        {
            CreatedAtUtc = result.CreatedAtUtc,
            CreatorDisplayName = calculation.CreatorDisplayName,
            AcceptorDisplayName = calculation.AcceptorDisplayName,
            Categories = calculation.Categories.Select(static value => value.ToString()).ToList(),
            Parks = calculation.Parks.Select(static park => new ProfileComparisonParkDto
            {
                Name = park.Name,
                CountryCode = park.CountryCode,
                CreatorVisitCount = park.CreatorVisitCount,
                AcceptorVisitCount = park.AcceptorVisitCount,
            }).ToList(),
            Ratings = calculation.Ratings.Select(static rating =>
                new ProfileComparisonRatingDto
                {
                    TargetType = rating.TargetType,
                    Name = rating.Name,
                    ParkName = rating.ParkName,
                    Category = rating.Category,
                    CreatorRating = rating.CreatorRating,
                    AcceptorRating = rating.AcceptorRating,
                    AbsoluteDifference = rating.AbsoluteDifference,
                    Affinity = rating.Affinity.ToString(),
                }).ToList(),
            Years = calculation.Years.Select(static year => new ProfileComparisonYearDto
            {
                Year = year.Year,
                CreatorVisitCount = year.CreatorVisitCount,
                AcceptorVisitCount = year.AcceptorVisitCount,
                CreatorRideCount = year.CreatorRideCount,
                AcceptorRideCount = year.AcceptorRideCount,
            }).ToList(),
            MissedItems = calculation.MissedItems.Select(static item =>
                new ProfileComparisonMissedItemDto
                {
                    Name = item.Name,
                    Status = item.Status,
                    CreatorOccurrenceCount = item.CreatorOccurrenceCount,
                    AcceptorOccurrenceCount = item.AcceptorOccurrenceCount,
                }).ToList(),
            CommonRatingCount = calculation.CommonRatingCount,
            MinimumRatingsForCorrelation = calculation.MinimumRatingsForCorrelation,
            RatingCorrelation = calculation.RatingCorrelation,
            HasIncompleteCatalog = calculation.HasIncompleteCatalog,
            CalculationVersion = calculation.CalculationVersion,
        };
    }

    public static ProfileComparisonSummaryDto ToHttp(this ProfileComparisonSummaryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ProfileComparisonSummaryDto
        {
            ShareId = result.ShareId,
            OtherDisplayName = result.OtherDisplayName,
            CreatedAtUtc = result.CreatedAtUtc,
            Categories = result.Categories.Select(static value => value.ToString()).ToList(),
        };
    }

    public static ProfileComparisonRevocationDto ToHttp(
        this ProfileComparisonRevocationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ProfileComparisonRevocationDto { RevokedAtUtc = result.RevokedAtUtc };
    }
}
