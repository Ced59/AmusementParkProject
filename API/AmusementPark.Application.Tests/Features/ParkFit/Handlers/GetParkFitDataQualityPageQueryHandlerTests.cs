using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class GetParkFitDataQualityPageQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldLoadOnlyTheRequestedParkPageAndBatchItsFacts()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc témoin",
            IsVisible = true,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        park.SetPosition(50, 3);
        ParkItem attraction = new ParkItem
        {
            Id = "item-1",
            ParkId = park.Id,
            Name = "Attraction témoin",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.FamilyRide,
            IsVisible = true,
            AttractionDetails = new AttractionDetails(),
        };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetPageAsync(
                2,
                10,
                true,
                null,
                null,
                null,
                null,
                null,
                ClosedEntityFilter.OpenOnly,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Name,
                false,
                null))
            .ReturnsAsync(new PagedResult<Park>(new[] { park }, 2, 10, 21));
        items.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { park.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { attraction });
        openingHours.Setup(repository => repository.GetSummariesByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { park.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>());
        GetParkFitDataQualityPageQueryHandler handler = new GetParkFitDataQualityPageQueryHandler(
            parks.Object,
            items.Object,
            openingHours.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkFitDataQualityAssessment>> result =
            await handler.HandleAsync(new GetParkFitDataQualityPageQuery(new PagedQuery(2, 10)));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(21, result.Value.TotalItems);
        ParkFitDataQualityAssessment assessment = Assert.Single(result.Value.Items);
        Assert.Equal(park.Id, assessment.ParkId);
        Assert.Equal(1, assessment.IssueItemCount);
        Assert.Contains(ParkFitDataQualityIssue.MissingAccessConditions, assessment.Issues);
        parks.VerifyAll();
        items.VerifyAll();
        openingHours.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPaginationIsInvalid_ShouldNotReadRepositories()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        GetParkFitDataQualityPageQueryHandler handler = new GetParkFitDataQualityPageQueryHandler(
            parks.Object,
            items.Object,
            openingHours.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkFitDataQualityAssessment>> result =
            await handler.HandleAsync(new GetParkFitDataQualityPageQuery(new PagedQuery(0, 10)));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        parks.VerifyNoOtherCalls();
        items.VerifyNoOtherCalls();
        openingHours.VerifyNoOtherCalls();
    }
}
