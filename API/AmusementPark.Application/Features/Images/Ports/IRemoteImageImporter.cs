using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Ports;

public interface IRemoteImageImporter
{
    Task<Image?> ImportAsync(RemoteImageImportRequest request, CancellationToken cancellationToken);
}
