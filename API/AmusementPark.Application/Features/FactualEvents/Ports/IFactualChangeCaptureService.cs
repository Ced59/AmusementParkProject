using AmusementPark.Application.Features.FactualEvents.Models;

namespace AmusementPark.Application.Features.FactualEvents.Ports;

public interface IFactualChangeCaptureService
{
    Task<FactualChangeCaptureResult> CaptureAfterCommitAsync(
        FactualChangeCaptureRequest request,
        CancellationToken cancellationToken);

    Task<FactualChangeCaptureResult> CapturePreparedAfterCommitAsync(
        FactualChangeOutboxEntry entry,
        CancellationToken cancellationToken);
}
