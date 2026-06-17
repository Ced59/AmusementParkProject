using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de création d'un parc.
/// </summary>
public sealed class CreateParkCommandHandler : ICommandHandler<CreateParkCommand, ApplicationResult<Park>>
{
    private readonly IParkRepository parkRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;

    public CreateParkCommandHandler(
        IParkRepository parkRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier)
    {
        this.parkRepository = parkRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult<Park>> HandleAsync(CreateParkCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Park is null)
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.Required(nameof(command.Park)));
        }

        try
        {
            Park created = await this.parkRepository.CreateAsync(command.Park, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, created.Id, cancellationToken);
            await this.publicSeoUpdateNotifier.NotifyAsync(
                new PublicSeoUpdate
                {
                    CurrentParks = PublicSeoParkSnapshot.FromParks(new[] { created }),
                    IncludeDiscoveryPages = true,
                },
                cancellationToken);
            return ApplicationResult<Park>.Success(created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<Park>.Failure(ParkApplicationErrors.ErrorCreatingPark());
        }
    }
}
