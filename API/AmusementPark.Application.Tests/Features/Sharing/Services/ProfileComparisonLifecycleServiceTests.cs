using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ProfileComparisonLifecycleServiceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RevokeAsync_ByParticipant_ShouldDisableExactComparisonVersion()
    {
        ProfileComparison comparison = CreateComparison();
        Mock<IProfileComparisonRepository> repository =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByShareTokenAsync(
                comparison.ShareToken,
                CancellationToken.None))
            .ReturnsAsync(comparison);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<ProfileComparison>(candidate =>
                    !candidate.IsActive && candidate.RevokedByUserId == "creator-1"),
                0,
                CancellationToken.None))
            .ReturnsAsync(ProfileComparisonWriteOutcome.Success);
        ProfileComparisonLifecycleService service = new ProfileComparisonLifecycleService(
            repository.Object,
            new SharePublicationFixedTimeProvider(NowUtc.AddMinutes(5)));

        ApplicationResult<ProfileComparisonRevocationResult> result = await service.RevokeAsync(
            "creator-1",
            comparison.ShareToken.Value,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NowUtc.AddMinutes(5), result.Value!.RevokedAtUtc);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RevokeAsync_ByOutsider_ShouldReturnNotFoundWithoutWriting()
    {
        ProfileComparison comparison = CreateComparison();
        Mock<IProfileComparisonRepository> repository =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByShareTokenAsync(
                comparison.ShareToken,
                CancellationToken.None))
            .ReturnsAsync(comparison);
        ProfileComparisonLifecycleService service = new ProfileComparisonLifecycleService(
            repository.Object,
            new SharePublicationFixedTimeProvider(NowUtc.AddMinutes(5)));

        ApplicationResult<ProfileComparisonRevocationResult> result = await service.RevokeAsync(
            "outsider",
            comparison.ShareToken.Value,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "profile-comparison.not-found");
        repository.VerifyAll();
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
