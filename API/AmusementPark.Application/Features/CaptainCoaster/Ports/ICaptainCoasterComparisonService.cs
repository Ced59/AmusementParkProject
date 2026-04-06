using AmusementPark.Application.Features.CaptainCoaster.Contracts;
using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Ports;

/// <summary>
/// Port applicatif de comparaison Captain Coaster.
/// </summary>
public interface ICaptainCoasterComparisonService
{
    Task<CaptainCoasterComparisonPreviewResult> PreviewAsync(CaptainCoasterSourceDescriptor source, CancellationToken cancellationToken);
}
