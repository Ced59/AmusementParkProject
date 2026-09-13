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

public sealed class UpdateParksBulkAdministrationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFilterScopeIsProvided_ShouldResolveIdsBeforeBulkUpdate()
    {
        UpdateParksBulkAdministrationCommandHandlerTestsFakeParkRepository parkRepository = new UpdateParksBulkAdministrationCommandHandlerTestsFakeParkRepository
        {
            AdministrationIds = new[] { "park-1", " park-2 ", "park-1", string.Empty },
            UpdatedCount = 2
        };
        UpdateParksBulkAdministrationCommandHandlerTestsFakeParkItemRepository parkItemRepository = new UpdateParksBulkAdministrationCommandHandlerTestsFakeParkItemRepository();
        FakeSearchProjectionWriter searchProjectionWriter = new FakeSearchProjectionWriter();
        UpdateParksBulkAdministrationCommandHandler handler = new UpdateParksBulkAdministrationCommandHandler(
            parkRepository,
            parkItemRepository,
            searchProjectionWriter,
            new NoOpPublicSeoUpdateNotifier());

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParksBulkAdministrationCommand(
                ParkIds: Array.Empty<string>(),
                IsVisible: true,
                AdminReviewStatus: null,
                FilterIsVisible: null,
                FilterAdminReviewStatus: AdminReviewStatus.ToReview,
                FilterType: ParkType.ThemePark,
                FilterCountryCode: "FR",
                FilterHasValidCoordinates: true,
                FilterAudienceClassification: ParkAudienceClassificationFilter.Unspecified));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.RequestedCount);
        Assert.Equal(2, result.Value.UpdatedCount);
        Assert.Single(parkRepository.GetAdministrationIdsCalls);
        Assert.Equal(ParkAudienceClassificationFilter.Unspecified, parkRepository.GetAdministrationIdsCalls.Single().AudienceClassificationFilter);
        Assert.Equal(new[] { "park-1", "park-2" }, parkRepository.UpdateCalls.Single().ParkIds);
        Assert.Equal(new[] { "parks:park-1", "parks:park-2" }, searchProjectionWriter.UpsertCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenNoIdsAndNoFilterScope_ShouldFail()
    {
        UpdateParksBulkAdministrationCommandHandlerTestsFakeParkRepository parkRepository = new UpdateParksBulkAdministrationCommandHandlerTestsFakeParkRepository();
        UpdateParksBulkAdministrationCommandHandler handler = new UpdateParksBulkAdministrationCommandHandler(
            parkRepository,
            new UpdateParksBulkAdministrationCommandHandlerTestsFakeParkItemRepository(),
            new FakeSearchProjectionWriter(),
            new NoOpPublicSeoUpdateNotifier());

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParksBulkAdministrationCommand(Array.Empty<string>(), true, null));

        Assert.False(result.IsSuccess);
        Assert.Empty(parkRepository.GetAdministrationIdsCalls);
        Assert.Empty(parkRepository.UpdateCalls);
    }












}
