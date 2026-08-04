using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class ParkOpeningHoursSitemapSectionProviderTests
{
    [Fact]
    public async Task GetUrlsAsync_ShouldOnlyPublishOpeningHoursForOperatingParks()
    {
        Park operating = CreatePark("operating", ParkStatus.Operating);
        Park planned = CreatePark("planned", ParkStatus.Planned);
        IReadOnlyCollection<Park> parks = new[] { operating, planned };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                500,
                false,
                true,
                null,
                null,
                null,
                null,
                ClosedEntityFilter.All,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Default,
                false,
                null))
            .ReturnsAsync(new PagedResult<Park>(parks, 1, 500, parks.Count));
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(repository => repository.GetSummariesByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.OrderBy(static id => id).SequenceEqual(new[] { "operating", "planned" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>
            {
                ["operating"] = CreateSummary("operating"),
                ["planned"] = CreateSummary("planned"),
            });
        ParkOpeningHoursSitemapSectionProvider provider = new ParkOpeningHoursSitemapSectionProvider(
            parkRepository.Object,
            openingHoursRepository.Object);

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(
            new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } },
            CancellationToken.None);

        Assert.Single(urls);
        Assert.Equal("/fr/park/operating/operating/opening-hours", urls.Single().RelativePath);
        parkRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
    }

    private static Park CreatePark(string id, ParkStatus status)
    {
        return new Park
        {
            Id = id,
            Name = id,
            IsVisible = true,
            Status = status,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }

    private static ParkOpeningHoursScheduleSummary CreateSummary(string parkId)
    {
        return new ParkOpeningHoursScheduleSummary
        {
            ParkId = parkId,
            HasScheduleData = true,
            UpdatedAtUtc = new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc),
        };
    }
}
