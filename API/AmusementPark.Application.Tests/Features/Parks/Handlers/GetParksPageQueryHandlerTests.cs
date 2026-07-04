using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Contracts;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetParksPageQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAudienceClassificationFilterIsProvided_ShouldPassItToRepository()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                12,
                false,
                true,
                null,
                null,
                null,
                null,
                ClosedEntityFilter.OpenOnly,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Default,
                false,
                ParkAudienceClassificationFilter.Unspecified))
            .ReturnsAsync(new PagedResult<Park>(Array.Empty<Park>(), 1, 12, 0));

        GetParksPageQueryHandler handler = new GetParksPageQueryHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            openingHoursRepository.Object,
            new ParkOpeningHoursAdminStatusResolver(),
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkListResult>> result = await handler.HandleAsync(
            new GetParksPageQuery(
                new PagedQuery(1, 12),
                IsVisible: true,
                AudienceClassificationFilter: ParkAudienceClassificationFilter.Unspecified));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.TotalItems);

        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        openingHoursRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenSortingByVisibleParkItems_ShouldSortCountsBeforePaging()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        List<Park> parks = new List<Park>
        {
            CreatePark("park-a", "Alpha"),
            CreatePark("park-b", "Beta"),
            CreatePark("park-c", "Gamma"),
        };

        parkRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                1,
                true,
                null,
                null,
                null,
                null,
                null,
                ClosedEntityFilter.All,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Default,
                false,
                null))
            .ReturnsAsync(new PagedResult<Park>(parks.Take(1).ToList(), 1, 1, parks.Count));

        parkRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                parks.Count,
                true,
                null,
                null,
                null,
                null,
                null,
                ClosedEntityFilter.All,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Default,
                true,
                null))
            .ReturnsAsync(new PagedResult<Park>(parks, 1, parks.Count, parks.Count));

        parkItemRepository
            .Setup(repository => repository.GetVisibilityCountsByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(new[] { "park-a", "park-b", "park-c" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkItemVisibilityCounts>
            {
                ["park-a"] = new ParkItemVisibilityCounts { TotalCount = 8, VisibleCount = 2 },
                ["park-b"] = new ParkItemVisibilityCounts { TotalCount = 4, VisibleCount = 4 },
                ["park-c"] = new ParkItemVisibilityCounts { TotalCount = 6, VisibleCount = 1 },
            });
        openingHoursRepository
            .Setup(repository => repository.GetSummariesByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(new[] { "park-a", "park-b", "park-c" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>(StringComparer.Ordinal));
        parkItemRepository
            .Setup(repository => repository.GetCountsByCategoryForParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(new[] { "park-a", "park-b", "park-c" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>(StringComparer.Ordinal));
        parkItemRepository
            .Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(new[] { "park-a", "park-b", "park-c" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        GetParksPageQueryHandler handler = new GetParksPageQueryHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            openingHoursRepository.Object,
            new ParkOpeningHoursAdminStatusResolver(),
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkListResult>> result = await handler.HandleAsync(
            new GetParksPageQuery(
                new PagedQuery(1, 2),
                IncludeHidden: true,
                ClosedFilter: ClosedEntityFilter.All,
                SortField: ParkAdminSortField.ParkItemsVisibleCount,
                SortDescending: true));

        Assert.True(result.IsSuccess);
        PagedResult<ParkListResult> page = Assert.IsType<PagedResult<ParkListResult>>(result.Value);
        Assert.Equal(new List<string> { "park-b", "park-a" }, page.Items.Select(static item => item.Park.Id ?? string.Empty).ToList());
        Assert.Equal(new List<int> { 4, 2 }, page.Items.Select(static item => item.ParkItemsVisibleCount ?? 0).ToList());
        Assert.Equal(3, page.TotalItems);

        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
    }

    private static Park CreatePark(string id, string name)
    {
        return new Park
        {
            Id = id,
            Name = name,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }
}
