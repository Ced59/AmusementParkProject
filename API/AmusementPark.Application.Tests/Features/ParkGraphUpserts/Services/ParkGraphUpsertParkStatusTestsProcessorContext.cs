using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

internal sealed class ParkGraphUpsertParkStatusTestsProcessorContext
{
    private readonly Func<Park?> savedParkAccessor;

    public ParkGraphUpsertParkStatusTestsProcessorContext(ParkGraphUpsertProcessor processor, Func<Park?> savedParkAccessor)
    {
        this.Processor = processor;
        this.savedParkAccessor = savedParkAccessor;
    }

    public ParkGraphUpsertProcessor Processor { get; }

    public Park? SavedPark => this.savedParkAccessor();
}
