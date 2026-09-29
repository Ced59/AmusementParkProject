using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveDataSourceCatalog
{
    bool IsPublicReadEnabled { get; }

    LiveDataSourcePresentation? Find(LiveDataSourceId sourceId);
}
