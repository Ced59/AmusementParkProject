using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ProfileComparisonReaderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListForParticipantAsync_ShouldExposeOnlyPublicTokenAndOtherDisplayName()
    {
        ProfileComparison comparison = CreateComparison();
        Mock<IProfileComparisonRepository> repository =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        repository.Setup(value => value.ListActiveByParticipantAsync(
                "creator-1",
                ProfileComparisonReader.ManagementListLimit,
                CancellationToken.None))
            .ReturnsAsync(new[] { comparison });
        ProfileComparisonReader reader = CreateReader(repository.Object);

        ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>> result =
            await reader.ListForParticipantAsync("creator-1", CancellationToken.None);

        ProfileComparisonSummaryResult summary = Assert.Single(result.Value!);
        Assert.Equal(comparison.ShareToken.Value, summary.ShareId);
        Assert.Equal("Alex", summary.OtherDisplayName);
        Assert.DoesNotContain(
            summary.GetType().GetProperties(),
            static property => property.Name.EndsWith("UserId", StringComparison.Ordinal)
                || property.Name.EndsWith("PublicationId", StringComparison.Ordinal));
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetSharedAsync_WhenRevoked_ShouldReturnNotFoundWithoutReadingPassports()
    {
        ProfileComparison comparison = CreateComparison();
        comparison.Revoke("acceptor-1", NowUtc.AddMinutes(1));
        Mock<IProfileComparisonRepository> repository =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByShareTokenAsync(
                comparison.ShareToken,
                CancellationToken.None))
            .ReturnsAsync(comparison);
        ProfileComparisonReader reader = CreateReader(repository.Object);

        ApplicationResult<SharedProfileComparisonResult> result = await reader.GetSharedAsync(
            comparison.ShareToken.Value,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "profile-comparison.not-found");
        repository.VerifyAll();
    }

    private static ProfileComparisonReader CreateReader(IProfileComparisonRepository repository)
    {
        ProfileComparisonPassportResolver resolver = new ProfileComparisonPassportResolver(
            Mock.Of<ISharePublicationRepository>(MockBehavior.Strict),
            Mock.Of<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict));
        return new ProfileComparisonReader(repository, resolver);
    }

    private static ProfileComparison CreateComparison()
    {
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
            new ProfileComparisonCalculation(
                "Camille",
                "Alex",
                new[] { ProfileComparisonCategory.VisitedParks },
                Array.Empty<ProfileComparisonParkResult>(),
                Array.Empty<ProfileComparisonRatingResult>(),
                Array.Empty<ProfileComparisonYearResult>(),
                Array.Empty<ProfileComparisonMissedItemResult>(),
                0,
                ProfileComparisonCalculator.MinimumRatingsForCorrelation,
                null,
                false,
                ProfileComparisonCalculator.CalculationVersion),
            NowUtc);
    }
}
