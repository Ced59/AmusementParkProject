using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Ports;

/// <summary>
/// Port applicatif d'accès aux paramètres Captain Coaster.
/// </summary>
public interface ICaptainCoasterSettingsRepository
{
    Task<CaptainCoasterSettingsResult> GetAsync(CancellationToken cancellationToken);
    Task<CaptainCoasterSettingsResult> UpdateAsync(CaptainCoasterSettingsResult settings, CancellationToken cancellationToken);
}
