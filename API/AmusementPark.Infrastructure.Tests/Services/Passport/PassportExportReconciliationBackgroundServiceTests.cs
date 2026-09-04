using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Infrastructure.Services.Passport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class PassportExportReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task ReconcileAsync_FailsStaleProcessingExportsBeforeSchedulingPendingOnes()
    {
        DateTime? maximumUpdatedAtUtc = null;
        DateTime? minimumExpiresAtUtc = null;
        DateTime? failedAtUtc = null;
        bool staleProcessingWasReconciled = false;
        Mock<IPassportExportRepository> repository =
            new Mock<IPassportExportRepository>(MockBehavior.Strict);
        repository.Setup(value => value.FailStaleProcessingAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                PassportExportErrorCodes.TimedOut,
                It.IsAny<DateTime>(),
                20,
                It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, string, DateTime, int, CancellationToken>(
                (maximumUpdated, minimumExpires, _, failedAt, _, _) =>
                {
                    maximumUpdatedAtUtc = maximumUpdated;
                    minimumExpiresAtUtc = minimumExpires;
                    failedAtUtc = failedAt;
                    staleProcessingWasReconciled = true;
                })
            .ReturnsAsync(1);
        repository.Setup(value => value.ListPendingForReconciliationAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                20,
                It.IsAny<CancellationToken>()))
            .Callback(() => Assert.True(staleProcessingWasReconciled))
            .ReturnsAsync(Array.Empty<PassportExport>());
        PassportExportScheduler scheduler = new PassportExportScheduler(
            Mock.Of<IDurableBackgroundJobRepository>(MockBehavior.Strict));
        ServiceCollection services = new ServiceCollection();
        services.AddScoped(_ => repository.Object);
        services.AddScoped(_ => scheduler);
        using ServiceProvider provider = services.BuildServiceProvider();
        PassportExportReconciliationBackgroundService service =
            new PassportExportReconciliationBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<PassportExportReconciliationBackgroundService>.Instance,
                TimeProvider.System);

        await service.ReconcileAsync(CancellationToken.None);

        Assert.NotNull(maximumUpdatedAtUtc);
        Assert.NotNull(minimumExpiresAtUtc);
        Assert.NotNull(failedAtUtc);
        Assert.Equal(failedAtUtc, minimumExpiresAtUtc);
        Assert.Equal(
            PassportExportReconciliationBackgroundService.MaximumProcessingAge,
            failedAtUtc.Value - maximumUpdatedAtUtc.Value);
        repository.VerifyAll();
    }
}
