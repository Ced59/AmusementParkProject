using AmusementPark.Application.Features.CaptainCoaster.Contracts;
using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Ports;

/// <summary>
/// Port applicatif d'orchestration des imports Captain Coaster.
/// </summary>
public interface ICaptainCoasterImportOrchestrator
{
    Task<CaptainCoasterSessionResult> StartAsync(CaptainCoasterSourceDescriptor source, CancellationToken cancellationToken);
    Task<CaptainCoasterSessionResult> ApplyAsync(string sessionId, IReadOnlyCollection<CaptainCoasterDuplicateResolution> resolutions, CancellationToken cancellationToken);
}
