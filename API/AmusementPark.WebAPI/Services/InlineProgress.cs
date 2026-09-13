using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.WebAPI.Services;

internal sealed class InlineProgress<TProgress> : IProgress<TProgress>
{
    private readonly Action<TProgress> handler;

    public InlineProgress(Action<TProgress> handler)
    {
        this.handler = handler;
    }

    public void Report(TProgress value)
    {
        this.handler(value);
    }
}
