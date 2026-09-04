using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class RequestPassportExportCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesPrivateExportAndSchedulesDurableJob()
    {
        DateTime nowUtc = new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc);
        Mock<IPassportExportRepository> exports = new Mock<IPassportExportRepository>(MockBehavior.Strict);
        PassportExport? created = null;
        exports.Setup(repository => repository.CreateAsync(
                It.IsAny<PassportExport>(),
                It.IsAny<CancellationToken>()))
            .Callback<PassportExport, CancellationToken>((value, _) => created = value)
            .Returns(Task.CompletedTask);
        Mock<IDurableBackgroundJobRepository> jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(repository => repository.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == PassportExportJob.Kind
                    && request.IdempotencyKey.StartsWith("passport-export:", StringComparison.Ordinal)
                    && request.PayloadVersion == PassportExportJob.PayloadVersion),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DurableBackgroundJob)null!);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(nowUtc);
        RequestPassportExportCommandHandler handler = new RequestPassportExportCommandHandler(
            exports.Object,
            new PassportExportScheduler(jobs.Object),
            clock.Object,
            NullLogger<RequestPassportExportCommandHandler>.Instance);

        AmusementPark.Application.Errors.ApplicationResult<PassportExport> result =
            await handler.HandleAsync(
                new RequestPassportExportCommand(" user-1 ", PassportExportFormat.Json));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("user-1", result.Value.UserId);
        Assert.Equal(PassportExportStatus.Pending, result.Value.Status);
        Assert.Equal(nowUtc.Add(PassportExportJob.Retention), result.Value.ExpiresAtUtc);
        Assert.Same(created, result.Value);
        jobs.VerifyAll();
        exports.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_RejectsUnknownFormatBeforePersistence()
    {
        Mock<IPassportExportRepository> exports = new Mock<IPassportExportRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        RequestPassportExportCommandHandler handler = new RequestPassportExportCommandHandler(
            exports.Object,
            new PassportExportScheduler(jobs.Object),
            clock.Object,
            NullLogger<RequestPassportExportCommandHandler>.Instance);

        AmusementPark.Application.Errors.ApplicationResult<PassportExport> result =
            await handler.HandleAsync(
                new RequestPassportExportCommand("user-1", (PassportExportFormat)99));

        Assert.False(result.IsSuccess);
        Assert.Equal("passport-export.format-invalid", Assert.Single(result.Errors).Code);
    }
}
