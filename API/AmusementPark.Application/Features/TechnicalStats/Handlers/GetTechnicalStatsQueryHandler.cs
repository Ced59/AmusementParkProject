using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalStats.Commands;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Application.Features.TechnicalStats.Queries;

namespace AmusementPark.Application.Features.TechnicalStats.Handlers;

public sealed class GetTechnicalStatsQueryHandler
    : IQueryHandler<GetTechnicalStatsQuery, ApplicationResult<TechnicalStatsSnapshot>>
{
    private readonly ITechnicalStatsProvider provider;

    public GetTechnicalStatsQueryHandler(ITechnicalStatsProvider provider)
    {
        this.provider = provider;
    }

    public async Task<ApplicationResult<TechnicalStatsSnapshot>> HandleAsync(
        GetTechnicalStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        TechnicalStatsSnapshot? snapshot = await this.provider.GetSnapshotAsync(cancellationToken);

        if (snapshot is null)
        {
            snapshot = new TechnicalStatsSnapshot
            {
                IsAvailable = false,
                GeneratedAtUtc = DateTime.UtcNow,
                StartedAtUtc = DateTime.UtcNow,
                UptimeSeconds = 0
            };
        }

        return ApplicationResult<TechnicalStatsSnapshot>.Success(snapshot);
    }
}

public sealed class UpdateTechnicalStatsSettingsCommandHandler
    : ICommandHandler<UpdateTechnicalStatsSettingsCommand, ApplicationResult<TechnicalStatsSettings>>
{
    private const int MinimumRetentionDays = 1;
    private const int MaximumRetentionDays = 365;

    private readonly ITechnicalStatsProvider provider;

    public UpdateTechnicalStatsSettingsCommandHandler(ITechnicalStatsProvider provider)
    {
        this.provider = provider;
    }

    public async Task<ApplicationResult<TechnicalStatsSettings>> HandleAsync(
        UpdateTechnicalStatsSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Settings.PersistenceRetentionDays < MinimumRetentionDays ||
            command.Settings.PersistenceRetentionDays > MaximumRetentionDays)
        {
            return ApplicationResult<TechnicalStatsSettings>.Failure(ApplicationError.Validation(
                "technical-stats.retention-days.invalid",
                "Technical stats retention days must be between 1 and 365."));
        }

        TechnicalStatsSettings? settings = await this.provider.UpdateSettingsAsync(command.Settings, cancellationToken);

        if (settings is null)
        {
            return ApplicationResult<TechnicalStatsSettings>.Failure(ApplicationError.Technical(
                "technical-stats.settings.unavailable",
                "SSR technical stats settings are unavailable."));
        }

        return ApplicationResult<TechnicalStatsSettings>.Success(settings);
    }
}
