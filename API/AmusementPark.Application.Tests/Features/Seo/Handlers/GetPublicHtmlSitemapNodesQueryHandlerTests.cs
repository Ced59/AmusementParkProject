using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.Videos.Ports;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Handlers;

public sealed class GetPublicHtmlSitemapNodesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParentIsRoot_ShouldReturnOnlyPublicCurrentLanguageNodes()
    {
        GetPublicHtmlSitemapNodesQueryHandler handler = CreateHandler();

        ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>> result = await handler.HandleAsync(
            new GetPublicHtmlSitemapNodesQuery("fr", null, new[] { "fr", "en" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value, static node => node.Id == "sitemap" && node.RelativeUrl == "/fr/sitemap");
        Assert.Contains(result.Value, static node => node.Id == "parks" && node.RelativeUrl == "/fr/parks" && node.HasChildren);
        Assert.Contains(result.Value, static node => node.Id == "technical" && node.RelativeUrl == "/fr/technical" && node.HasChildren);
        Assert.DoesNotContain(result.Value, static node => node.Id.Contains("admin", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Value, static node => node.RelativeUrl?.Contains("/login", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(result.Value, static node => node.RelativeUrl?.Contains("/profile", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageIsNotSupported_ShouldReturnValidationError()
    {
        GetPublicHtmlSitemapNodesQueryHandler handler = CreateHandler();

        ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>> result = await handler.HandleAsync(
            new GetPublicHtmlSitemapNodesQuery("jp", null, new[] { "fr", "en" }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "seo.html-sitemap.language.invalid");
    }

    private static GetPublicHtmlSitemapNodesQueryHandler CreateHandler()
    {
        return new GetPublicHtmlSitemapNodesQueryHandler(
            Mock.Of<IParkRepository>(),
            Mock.Of<IParkItemRepository>(),
            Mock.Of<IParkZoneRepository>(),
            Mock.Of<IParkOpeningHoursRepository>(),
            Mock.Of<IImageRepository>(),
            Mock.Of<IVideoRepository>(),
            Mock.Of<IHistoryEventRepository>(),
            Mock.Of<IParkOperatorRepository>(),
            Mock.Of<IParkFounderRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<ITechnicalPageRepository>());
    }
}
