using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IShareTokenFactory
{
    ShareToken Generate();
}
