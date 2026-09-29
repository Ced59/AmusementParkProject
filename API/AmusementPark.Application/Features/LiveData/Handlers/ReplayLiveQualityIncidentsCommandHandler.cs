using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class ReplayLiveQualityIncidentsCommandHandler
    : ICommandHandler<
        ReplayLiveQualityIncidentsCommand,
        ApplicationResult<LiveQualityReplayResult>>
{
    private readonly LiveQualityIncidentReplayService replayService;

    public ReplayLiveQualityIncidentsCommandHandler(
        LiveQualityIncidentReplayService replayService)
    {
        this.replayService = replayService;
    }

    public async Task<ApplicationResult<LiveQualityReplayResult>> HandleAsync(
        ReplayLiveQualityIncidentsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.MaximumCount is < 1 or > 100
            || string.IsNullOrWhiteSpace(command.AdministratorUserId)
            || command.AdministratorUserId.Trim().Length > IdentifierRules.MaximumLength
            || command.AdministratorUserId.Any(char.IsControl))
        {
            return ApplicationResult<LiveQualityReplayResult>.Failure(
                LiveDataApplicationErrors.InvalidReplay());
        }

        LiveQualityReplayResult result = await this.replayService.ReplayAsync(
            command.MaximumCount,
            command.AdministratorUserId,
            cancellationToken);
        return ApplicationResult<LiveQualityReplayResult>.Success(result);
    }
}
