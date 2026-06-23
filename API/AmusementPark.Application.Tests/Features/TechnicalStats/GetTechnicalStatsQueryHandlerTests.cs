using AmusementPark.Application.Features.TechnicalStats.Commands;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Handlers;
using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Application.Features.TechnicalStats.Queries;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.TechnicalStats;

public sealed class GetTechnicalStatsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsyncReturnsSnapshotWhenProviderSucceeds()
    {
        TechnicalStatsSnapshot snapshot = new TechnicalStatsSnapshot
        {
            BuildVersion = "2.6.18",
            GeneratedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            UptimeSeconds = 600
        };
        Mock<ITechnicalStatsProvider> provider = new Mock<ITechnicalStatsProvider>(MockBehavior.Strict);
        provider
            .Setup(candidate => candidate.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        GetTechnicalStatsQueryHandler handler = new GetTechnicalStatsQueryHandler(provider.Object);

        AmusementPark.Application.Errors.ApplicationResult<TechnicalStatsSnapshot> result = await handler.HandleAsync(new GetTechnicalStatsQuery());

        Assert.True(result.IsSuccess);
        Assert.Same(snapshot, result.Value);
    }

    [Fact]
    public async Task HandleAsyncReturnsUnavailableSnapshotWhenProviderReturnsNull()
    {
        Mock<ITechnicalStatsProvider> provider = new Mock<ITechnicalStatsProvider>(MockBehavior.Strict);
        provider
            .Setup(candidate => candidate.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TechnicalStatsSnapshot?)null);
        GetTechnicalStatsQueryHandler handler = new GetTechnicalStatsQueryHandler(provider.Object);

        AmusementPark.Application.Errors.ApplicationResult<TechnicalStatsSnapshot> result = await handler.HandleAsync(new GetTechnicalStatsQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsAvailable);
        Assert.Null(result.Value.UnavailableReason);
        Assert.Equal(0, result.Value.UptimeSeconds);
    }

    [Fact]
    public async Task UpdateSettingsReturnsValidationFailureWhenRetentionIsOutOfRange()
    {
        Mock<ITechnicalStatsProvider> provider = new Mock<ITechnicalStatsProvider>(MockBehavior.Strict);
        UpdateTechnicalStatsSettingsCommandHandler handler = new UpdateTechnicalStatsSettingsCommandHandler(provider.Object);

        AmusementPark.Application.Errors.ApplicationResult<TechnicalStatsSettings> result = await handler.HandleAsync(
            new UpdateTechnicalStatsSettingsCommand(new TechnicalStatsSettings { PersistenceRetentionDays = 0 }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "technical-stats.retention-days.invalid");
    }

    [Fact]
    public async Task UpdateSettingsReturnsUpdatedSettingsWhenProviderSucceeds()
    {
        TechnicalStatsSettings settings = new TechnicalStatsSettings { PersistenceRetentionDays = 15 };
        Mock<ITechnicalStatsProvider> provider = new Mock<ITechnicalStatsProvider>(MockBehavior.Strict);
        provider
            .Setup(item => item.UpdateSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        UpdateTechnicalStatsSettingsCommandHandler handler = new UpdateTechnicalStatsSettingsCommandHandler(provider.Object);

        AmusementPark.Application.Errors.ApplicationResult<TechnicalStatsSettings> result = await handler.HandleAsync(new UpdateTechnicalStatsSettingsCommand(settings));

        Assert.True(result.IsSuccess);
        Assert.Same(settings, result.Value);
    }

    [Fact]
    public async Task UpdateSettingsReturnsFailureWhenProviderReturnsNull()
    {
        TechnicalStatsSettings settings = new TechnicalStatsSettings { PersistenceRetentionDays = 15 };
        Mock<ITechnicalStatsProvider> provider = new Mock<ITechnicalStatsProvider>(MockBehavior.Strict);
        provider
            .Setup(item => item.UpdateSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TechnicalStatsSettings?)null);
        UpdateTechnicalStatsSettingsCommandHandler handler = new UpdateTechnicalStatsSettingsCommandHandler(provider.Object);

        AmusementPark.Application.Errors.ApplicationResult<TechnicalStatsSettings> result = await handler.HandleAsync(new UpdateTechnicalStatsSettingsCommand(settings));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "technical-stats.settings.unavailable");
    }
}
