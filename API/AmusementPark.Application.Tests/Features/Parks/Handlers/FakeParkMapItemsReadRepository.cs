using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

internal sealed class FakeParkMapItemsReadRepository : IParkMapItemsReadRepository
{
    public ParkMapItemsResult? MapItems { get; init; }

    public List<MapItemsCall> Calls { get; } = new List<MapItemsCall>();

    public Task<ParkMapItemsResult?> GetAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        this.Calls.Add(new MapItemsCall(parkId, includeHidden, closedFilter));
        return Task.FromResult(this.MapItems);
    }
}
