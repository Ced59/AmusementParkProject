using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class DownloadPassportExportQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsOnlyTheAuthenticatedOwnersReadyArtifact()
    {
        DateTime nowUtc = new DateTime(2026, 9, 4, 15, 0, 0, DateTimeKind.Utc);
        string exportId = "0123456789abcdef0123456789abcdef";
        PassportExportDownload download = new PassportExportDownload(
            "passport.json",
            "application/json",
            new byte[] { 1, 2, 3 },
            new string('a', 64));
        Mock<IPassportExportRepository> repository = new Mock<IPassportExportRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedDownloadAsync(
                exportId,
                "owner-1",
                nowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(download);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(nowUtc);
        DownloadPassportExportQueryHandler handler = new DownloadPassportExportQueryHandler(
            repository.Object,
            clock.Object);

        AmusementPark.Application.Errors.ApplicationResult<PassportExportDownload> result =
            await handler.HandleAsync(new DownloadPassportExportQuery(" owner-1 ", $" {exportId} "));

        Assert.True(result.IsSuccess);
        Assert.Same(download, result.Value);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_InvalidOpaqueIdentifierDoesNotReachPersistence()
    {
        Mock<IPassportExportRepository> repository = new Mock<IPassportExportRepository>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        DownloadPassportExportQueryHandler handler = new DownloadPassportExportQueryHandler(
            repository.Object,
            clock.Object);

        AmusementPark.Application.Errors.ApplicationResult<PassportExportDownload> result =
            await handler.HandleAsync(new DownloadPassportExportQuery("owner-1", "predictable"));

        Assert.False(result.IsSuccess);
        Assert.Equal("passport-export.not-found", Assert.Single(result.Errors).Code);
        repository.VerifyNoOtherCalls();
        clock.VerifyNoOtherCalls();
    }
}
