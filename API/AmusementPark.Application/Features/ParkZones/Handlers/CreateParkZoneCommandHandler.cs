using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class CreateParkZoneCommandHandler : ICommandHandler<CreateParkZoneCommand, ApplicationResult<ParkZone>>
{
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkRepository parkRepository;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public CreateParkZoneCommandHandler(
        IParkZoneRepository parkZoneRepository,
        IParkRepository parkRepository,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.parkZoneRepository = parkZoneRepository;
        this.parkRepository = parkRepository;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult<ParkZone>> HandleAsync(CreateParkZoneCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Zone is null)
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.Required(nameof(command.Zone)));
        }

        if (string.IsNullOrWhiteSpace(command.Zone.ParkId))
        {
            return ApplicationResult<ParkZone>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        Park? park = await this.parkRepository.GetByIdAsync(command.Zone.ParkId.Trim(), true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkZone>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        try
        {
            ParkZoneNaming.Normalize(command.Zone);
            ParkZone created = await this.parkZoneRepository.CreateAsync(command.Zone, cancellationToken);
            await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
            return ApplicationResult<ParkZone>.Success(created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<ParkZone>.Failure(ParkZoneApplicationErrors.ErrorCreatingParkZone());
        }
    }
}
