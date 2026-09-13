using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ProfileComparisonMongoMapperTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Mapping_ShouldRoundTripFrozenCalculationAndRevocation()
    {
        ProfileComparison comparison = CreateComparison();
        comparison.Revoke("acceptor-1", NowUtc.AddMinutes(4));

        ProfileComparisonDocument document = comparison.ToDocument();
        ProfileComparison restored = document.ToDomain();

        Assert.Equal(ProfileComparisonStatus.Revoked, restored.Status);
        Assert.Equal("acceptor-1", restored.RevokedByUserId);
        ProfileComparisonParkResult park = Assert.Single(restored.Calculation.Parks);
        Assert.Equal("Phantasialand", park.Name);
        Assert.Equal(2, park.CreatorVisitCount);
        Assert.Equal(4, park.AcceptorVisitCount);
        Assert.Equal(ProfileComparisonCalculator.CalculationVersion,
            restored.Calculation.CalculationVersion);
    }

    [Fact]
    public void Mapping_ShouldRoundTripModerationSuspensionWithoutRevokingComparison()
    {
        ProfileComparison comparison = CreateComparison();
        ShareModerationReportId reportId = ShareModerationReportId.Parse("report-1");
        comparison.SuspendByModeration(reportId, NowUtc.AddMinutes(1));

        ProfileComparison restored = comparison.ToDocument().ToDomain();

        Assert.True(restored.IsActive);
        Assert.True(restored.IsModerationSuspended);
        Assert.True(restored.HasModerationSuspension(reportId));
        Assert.False(restored.IsPubliclyResolvable);
    }

    private static ProfileComparison CreateComparison()
    {
        ProfileComparisonCalculation calculation = new ProfileComparisonCalculation(
            "Camille",
            "Alex",
            new[] { ProfileComparisonCategory.VisitedParks },
            new[] { new ProfileComparisonParkResult("Phantasialand", "DE", 2, 4) },
            Array.Empty<ProfileComparisonRatingResult>(),
            Array.Empty<ProfileComparisonYearResult>(),
            Array.Empty<ProfileComparisonMissedItemResult>(),
            0,
            ProfileComparisonCalculator.MinimumRatingsForCorrelation,
            null,
            false,
            ProfileComparisonCalculator.CalculationVersion);
        return ProfileComparison.Create(
            ProfileComparisonId.Parse("comparison-1"),
            ProfileComparisonInvitationId.Parse("invitation-1"),
            ShareToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8"),
            "creator-1",
            "acceptor-1",
            SharePublicationId.Parse("creator-passport"),
            1,
            SharePublicationId.Parse("acceptor-passport"),
            2,
            calculation,
            NowUtc);
    }
}
