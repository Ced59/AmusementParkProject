using System.Text.Json;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

internal sealed class ParkGraphPricingTestsProcessorContext
{
    public ParkGraphPricingTestsProcessorContext(
        ParkGraphUpsertProcessor processor,
        Mock<IParkRepository> parkRepository,
        Mock<ISearchProjectionWriter> searchProjectionWriter,
        Mock<IParkGraphUpsertHistoryRepository> historyRepository,
        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier)
    {
        this.Processor = processor;
        this.ParkRepository = parkRepository;
        this.SearchProjectionWriter = searchProjectionWriter;
        this.HistoryRepository = historyRepository;
        this.PublicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public ParkGraphUpsertProcessor Processor { get; }

    private Mock<IParkRepository> ParkRepository { get; }

    private Mock<ISearchProjectionWriter> SearchProjectionWriter { get; }

    private Mock<IParkGraphUpsertHistoryRepository> HistoryRepository { get; }

    private Mock<IPublicSeoUpdateNotifier> PublicSeoUpdateNotifier { get; }

    public void VerifyAll()
    {
        this.ParkRepository.VerifyAll();
        this.SearchProjectionWriter.VerifyAll();
        this.HistoryRepository.VerifyAll();
        this.PublicSeoUpdateNotifier.VerifyAll();
    }
}
