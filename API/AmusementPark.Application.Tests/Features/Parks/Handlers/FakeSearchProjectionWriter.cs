using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

internal sealed class FakeSearchProjectionWriter : ISearchProjectionWriter
{
    public List<string> UpsertCalls { get; } = new List<string>();

    public Task UpsertAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
    {
        this.UpsertCalls.Add($"{resourceType}:{resourceId}");
        return Task.CompletedTask;
    }

    public Task UpsertManyAsync(string resourceType, IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
