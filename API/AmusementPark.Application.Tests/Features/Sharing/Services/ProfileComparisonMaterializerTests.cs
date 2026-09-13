using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ProfileComparisonMaterializerTests
{
    private const string CreatorUserId = "creator-1";
    private const string AcceptorUserId = "acceptor-1";
    private const string Token = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime CreatedAtUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task MaterializeAsync_WhenExistingComparisonWasRevoked_ShouldNotReturnDeadLink()
    {
        ProfileComparisonInvitation invitation = CreateAcceptedInvitation();
        ProfileComparison comparison = CreateComparison(invitation);
        comparison.Revoke(CreatorUserId, CreatedAtUtc.AddMinutes(1));
        Mock<IProfileComparisonRepository> comparisons = CreateComparisonRepository(
            invitation,
            comparison);
        ProfileComparisonMaterializer materializer = CreateMaterializer(
            comparisons.Object,
            new Mock<ISharePublicationRepository>(MockBehavior.Strict).Object,
            new Mock<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict).Object);

        ApplicationResult<ProfileComparison> result = await materializer.MaterializeAsync(
            invitation,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "profile-comparison.unavailable");
        comparisons.VerifyAll();
    }

    [Fact]
    public async Task MaterializeAsync_WhenExistingPassportVersionIsUnavailable_ShouldNotReturnDeadLink()
    {
        ProfileComparisonInvitation invitation = CreateAcceptedInvitation();
        ProfileComparison comparison = CreateComparison(invitation);
        Mock<IProfileComparisonRepository> comparisons = CreateComparisonRepository(
            invitation,
            comparison);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetOwnedAsync(
                comparison.CreatorPassportPublicationId,
                CreatorUserId,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        publications.Setup(value => value.GetOwnedAsync(
                comparison.AcceptorPassportPublicationId,
                AcceptorUserId,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        ProfileComparisonMaterializer materializer = CreateMaterializer(
            comparisons.Object,
            publications.Object,
            new Mock<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict).Object);

        ApplicationResult<ProfileComparison> result = await materializer.MaterializeAsync(
            invitation,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "profile-comparison.passports-changed");
        comparisons.VerifyAll();
        publications.VerifyAll();
    }

    private static ProfileComparisonMaterializer CreateMaterializer(
        IProfileComparisonRepository comparisons,
        ISharePublicationRepository publications,
        IPassportProfileShareSnapshotRepository snapshots)
    {
        ProfileComparisonPassportResolver resolver = new ProfileComparisonPassportResolver(
            publications,
            snapshots);
        return new ProfileComparisonMaterializer(
            comparisons,
            resolver,
            new Mock<IShareTokenFactory>(MockBehavior.Strict).Object);
    }

    private static Mock<IProfileComparisonRepository> CreateComparisonRepository(
        ProfileComparisonInvitation invitation,
        ProfileComparison comparison)
    {
        Mock<IProfileComparisonRepository> repository =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(
                invitation.ComparisonId!.Value,
                CancellationToken.None))
            .ReturnsAsync(comparison);
        return repository;
    }

    private static ProfileComparisonInvitation CreateAcceptedInvitation()
    {
        ProfileComparisonInvitation invitation = ProfileComparisonInvitation.Create(
            ProfileComparisonInvitationId.New(),
            ShareToken.Parse(Token),
            CreatorUserId,
            SharePublicationId.Parse("creator-passport"),
            1,
            new[] { ProfileComparisonCategory.VisitedParks },
            CreatedAtUtc,
            CreatedAtUtc.AddDays(7));
        invitation.Accept(
            AcceptorUserId,
            SharePublicationId.Parse("acceptor-passport"),
            1,
            ProfileComparisonId.New(),
            CreatedAtUtc);
        return invitation;
    }

    private static ProfileComparison CreateComparison(ProfileComparisonInvitation invitation)
    {
        ProfileComparisonCalculation calculation = new ProfileComparisonCalculation(
            "Camille",
            "Alex",
            invitation.Categories,
            Array.Empty<ProfileComparisonParkResult>(),
            Array.Empty<ProfileComparisonRatingResult>(),
            Array.Empty<ProfileComparisonYearResult>(),
            Array.Empty<ProfileComparisonMissedItemResult>(),
            0,
            5,
            null,
            false,
            "profile-comparison-v1");
        return ProfileComparison.Create(
            invitation.ComparisonId!.Value,
            invitation.Id,
            ShareToken.Parse(Token),
            CreatorUserId,
            AcceptorUserId,
            invitation.CreatorPassportPublicationId,
            invitation.CreatorPassportPublicationVersion,
            invitation.AcceptorPassportPublicationId!.Value,
            invitation.AcceptorPassportPublicationVersion!.Value,
            calculation,
            CreatedAtUtc);
    }
}
