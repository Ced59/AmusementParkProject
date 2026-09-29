namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILivePublicExperienceGate
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken);
}
