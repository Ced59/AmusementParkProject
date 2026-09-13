using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class ProfileComparisonMongoMapper
{
    public static ProfileComparisonDocument ToDocument(this ProfileComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        return new ProfileComparisonDocument
        {
            Id = comparison.Id.Value,
            InvitationId = comparison.InvitationId.Value,
            ShareToken = comparison.ShareToken.Value,
            CreatorUserId = comparison.CreatorUserId,
            AcceptorUserId = comparison.AcceptorUserId,
            CreatorPassportPublicationId = comparison.CreatorPassportPublicationId.Value,
            CreatorPassportPublicationVersion = comparison.CreatorPassportPublicationVersion,
            AcceptorPassportPublicationId = comparison.AcceptorPassportPublicationId.Value,
            AcceptorPassportPublicationVersion = comparison.AcceptorPassportPublicationVersion,
            Calculation = ToDocument(comparison.Calculation),
            Status = comparison.Status,
            RevokedByUserId = comparison.RevokedByUserId,
            RevokedAtUtc = comparison.RevokedAtUtc,
            Version = comparison.Version,
            ModerationSuspensionReportId = comparison.ModerationSuspensionReportId?.Value,
            CreatedAt = comparison.CreatedAtUtc,
            UpdatedAt = comparison.UpdatedAtUtc,
        };
    }

    public static ProfileComparison ToDomain(this ProfileComparisonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ProfileComparison.Restore(
            ProfileComparisonId.Parse(document.Id),
            ProfileComparisonInvitationId.Parse(document.InvitationId),
            ShareToken.Parse(document.ShareToken),
            document.CreatorUserId,
            document.AcceptorUserId,
            SharePublicationId.Parse(document.CreatorPassportPublicationId),
            document.CreatorPassportPublicationVersion,
            SharePublicationId.Parse(document.AcceptorPassportPublicationId),
            document.AcceptorPassportPublicationVersion,
            ToDomain(document.Calculation),
            document.Status,
            document.RevokedByUserId,
            document.RevokedAtUtc,
            document.CreatedAt,
            document.UpdatedAt,
            document.Version,
            document.ModerationSuspensionReportId is null
                ? null
                : ShareModerationReportId.Parse(document.ModerationSuspensionReportId));
    }

    private static ProfileComparisonCalculationDocument ToDocument(
        ProfileComparisonCalculation calculation)
    {
        return new ProfileComparisonCalculationDocument
        {
            CreatorDisplayName = calculation.CreatorDisplayName,
            AcceptorDisplayName = calculation.AcceptorDisplayName,
            Categories = calculation.Categories.ToList(),
            Parks = calculation.Parks.Select(static park => new ProfileComparisonParkDocument
            {
                Name = park.Name,
                CountryCode = park.CountryCode,
                CreatorVisitCount = park.CreatorVisitCount,
                AcceptorVisitCount = park.AcceptorVisitCount,
            }).ToList(),
            Ratings = calculation.Ratings.Select(static rating =>
                new ProfileComparisonRatingDocument
                {
                    TargetType = rating.TargetType,
                    Name = rating.Name,
                    ParkName = rating.ParkName,
                    Category = rating.Category,
                    CreatorRating = rating.CreatorRating,
                    AcceptorRating = rating.AcceptorRating,
                    AbsoluteDifference = rating.AbsoluteDifference,
                    Affinity = rating.Affinity,
                }).ToList(),
            Years = calculation.Years.Select(static year => new ProfileComparisonYearDocument
            {
                Year = year.Year,
                CreatorVisitCount = year.CreatorVisitCount,
                AcceptorVisitCount = year.AcceptorVisitCount,
                CreatorRideCount = year.CreatorRideCount,
                AcceptorRideCount = year.AcceptorRideCount,
            }).ToList(),
            MissedItems = calculation.MissedItems.Select(static item =>
                new ProfileComparisonMissedItemDocument
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

    private static ProfileComparisonCalculation ToDomain(
        ProfileComparisonCalculationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new ProfileComparisonCalculation(
            document.CreatorDisplayName,
            document.AcceptorDisplayName,
            document.Categories,
            document.Parks.Select(static park => new ProfileComparisonParkResult(
                park.Name,
                park.CountryCode,
                park.CreatorVisitCount,
                park.AcceptorVisitCount)).ToArray(),
            document.Ratings.Select(static rating => new ProfileComparisonRatingResult(
                rating.TargetType,
                rating.Name,
                rating.ParkName,
                rating.Category,
                rating.CreatorRating,
                rating.AcceptorRating,
                rating.AbsoluteDifference,
                rating.Affinity)).ToArray(),
            document.Years.Select(static year => new ProfileComparisonYearResult(
                year.Year,
                year.CreatorVisitCount,
                year.AcceptorVisitCount,
                year.CreatorRideCount,
                year.AcceptorRideCount)).ToArray(),
            document.MissedItems.Select(static item => new ProfileComparisonMissedItemResult(
                item.Name,
                item.Status,
                item.CreatorOccurrenceCount,
                item.AcceptorOccurrenceCount)).ToArray(),
            document.CommonRatingCount,
            document.MinimumRatingsForCorrelation,
            document.RatingCorrelation,
            document.HasIncompleteCatalog,
            document.CalculationVersion);
    }
}
