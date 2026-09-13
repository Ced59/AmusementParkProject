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

internal sealed class FakeParkDetailSummaryReadRepository : IParkDetailSummaryReadRepository
{
    public ParkDetailSummaryResult? Summary { get; init; }

    public List<SummaryCall> Calls { get; } = new List<SummaryCall>();

    public Task<ParkDetailSummaryResult?> GetAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        this.Calls.Add(new SummaryCall(parkId, includeHidden, closedFilter));
        return Task.FromResult(this.Summary);
    }
}
