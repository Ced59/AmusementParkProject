using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileShareSnapshotWriterTests
{
    private static readonly DateTime Now =
        new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task WriteAsync_ShouldPersistTheCompleteFrozenParkSelection()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            new[] { ShareContentField.RideCount });
        PassportProfileShareInput input = new PassportProfileShareInput(
            new[] { 2026 },
            new[] { "park-1", "park-2" },
            Array.Empty<string>(),
            null,
            ShareVisibility.Unlisted,
            false);
        PassportProfileSharePreviewResult content = new PassportProfileSharePreviewResult(
            null,
            null,
            null,
            ShareVisibility.Unlisted,
            false,
            2,
            1,
            3,
            2,
            null,
            null,
            Array.Empty<PassportProfileShareCountryResult>(),
            Array.Empty<PassportProfileShareYearResult>(),
            Array.Empty<PassportProfileShareParkResult>(),
            Array.Empty<PassportProfileShareRatingResult>(),
            Array.Empty<PassportProfileShareMissedItemResult>(),
            false,
            "passport-profile-v1",
            false);
        PassportProfileShareSelectedParkSnapshot[] selectedParks =
        {
            new PassportProfileShareSelectedParkSnapshot("park-1", "Parc un", "FR"),
            new PassportProfileShareSelectedParkSnapshot("park-2", "Parc deux", "BE"),
        };
        SharePublicationPreviewResult preview = new SharePublicationPreviewResult(
            SharePublicationType.PassportProfile,
            6,
            policy.SchemaVersion,
            policy.DatePrecision,
            policy.IncludedFields,
            null,
            ContentFingerprint: "fingerprint",
            PassportProfile: content)
        {
            PassportProfileSelectedParks = selectedParks,
        };
        Mock<IPassportProfileSharePreviewBuilder> builder =
            new Mock<IPassportProfileSharePreviewBuilder>(MockBehavior.Strict);
        builder.Setup(value => value.BuildAsync(
                "owner-1",
                policy,
                input,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationPreviewResult>.Success(preview));
        Mock<IPassportProfileShareSnapshotRepository> snapshots =
            new Mock<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.UpsertAsync(
                It.Is<PassportProfileShareSnapshot>(snapshot =>
                    snapshot.SelectedParks.SequenceEqual(selectedParks)
                    && snapshot.CreatedAtUtc == Now),
                CancellationToken.None))
            .ReturnsAsync(true);
        PassportProfileShareSnapshotWriter writer = new PassportProfileShareSnapshotWriter(
            builder.Object,
            snapshots.Object,
            new SharePublicationFixedTimeProvider(Now));

        ApplicationResult<bool> result = await writer.WriteAsync(
            new SharePublicationSnapshotWriteRequest(
                SharePublicationId.Parse("publication-1"),
                1,
                1,
                "owner-1",
                null,
                6,
                policy,
                "fingerprint",
                PassportProfile: input),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        builder.VerifyAll();
        snapshots.VerifyAll();
    }
}
