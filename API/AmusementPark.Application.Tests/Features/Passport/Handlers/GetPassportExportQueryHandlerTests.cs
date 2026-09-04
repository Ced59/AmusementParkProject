using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class GetPassportExportQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReportsExpiredWithoutExposingAnotherOwner()
    {
        DateTime nowUtc = new DateTime(2026, 9, 4, 13, 0, 0, DateTimeKind.Utc);
        string exportId = "0123456789abcdef0123456789abcdef";
        PassportExport passportExport = new PassportExport(
            exportId,
            "user-1",
            PassportExportFormat.Json,
            PassportExportStatus.Ready,
            1,
            nowUtc.AddHours(-2),
            nowUtc.AddHours(-2),
            nowUtc.AddMinutes(-1));
        Mock<IPassportExportRepository> repository = new Mock<IPassportExportRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(exportId, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(passportExport);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(nowUtc);
        GetPassportExportQueryHandler handler = new GetPassportExportQueryHandler(repository.Object, clock.Object);

        AmusementPark.Application.Errors.ApplicationResult<PassportExport> result =
            await handler.HandleAsync(new GetPassportExportQuery("user-1", exportId));

        Assert.True(result.IsSuccess);
        Assert.Equal(PassportExportStatus.Expired, result.Value?.Status);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_InvalidIdentifierReturnsSameNotFoundError()
    {
        Mock<IPassportExportRepository> repository = new Mock<IPassportExportRepository>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        GetPassportExportQueryHandler handler = new GetPassportExportQueryHandler(repository.Object, clock.Object);

        AmusementPark.Application.Errors.ApplicationResult<PassportExport> result =
            await handler.HandleAsync(new GetPassportExportQuery("user-1", "predictable"));

        Assert.False(result.IsSuccess);
        Assert.Equal("passport-export.not-found", Assert.Single(result.Errors).Code);
    }
}
