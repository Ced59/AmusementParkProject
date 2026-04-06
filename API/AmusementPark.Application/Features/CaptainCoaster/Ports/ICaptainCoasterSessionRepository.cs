using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Ports;

/// <summary>
/// Port applicatif d'accès aux sessions Captain Coaster.
/// </summary>
public interface ICaptainCoasterSessionRepository
{
    Task<CaptainCoasterSessionResult?> GetByIdAsync(string sessionId, CancellationToken cancellationToken);
}
