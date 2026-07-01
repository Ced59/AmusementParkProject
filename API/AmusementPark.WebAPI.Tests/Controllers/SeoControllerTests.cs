using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class SeoControllerTests
{
    [Fact]
    public void GetRobotsTxt_WhenPublicImageAllowPathIsConfigured_ShouldAllowImagesAndKeepApiDisallowed()
    {
        SeoController controller = CreateController(new SeoSettings
        {
            PublicBaseUrl = "https://amusement-parks.fun",
            SupportedLanguages = new List<string> { "en", "fr" },
            RobotsAllowPaths = new List<string> { "/api/images/" },
            RobotsDisallowPaths = new List<string> { "/api/", "/{lang}/admin/" },
        });

        ContentResult result = Assert.IsType<ContentResult>(controller.GetRobotsTxt());
        string content = Assert.IsType<string>(result.Content);
        string[] lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("Allow: /", lines);
        Assert.Contains("Allow: /api/images/", lines);
        Assert.Contains("Disallow: /api/", lines);
        Assert.Contains("Disallow: /en/admin/", lines);
        Assert.Contains("Disallow: /fr/admin/", lines);
        Assert.Contains("Sitemap: https://amusement-parks.fun/sitemap.xml", lines);
        Assert.True(content.IndexOf("Allow: /api/images/", StringComparison.Ordinal) < content.IndexOf("Disallow: /api/", StringComparison.Ordinal));
    }

    private static SeoController CreateController(SeoSettings settings)
    {
        Mock<IWebHostEnvironment> environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(static candidate => candidate.EnvironmentName).Returns(Environments.Production);

        Mock<ISeoSitemapSettingsRepository> sitemapSettingsRepository = new Mock<ISeoSitemapSettingsRepository>();
        Mock<IQueryHandler<GetPublicSitemapDocumentQuery, ApplicationResult<SitemapDocumentResult>>> sitemapDocumentHandler = new Mock<IQueryHandler<GetPublicSitemapDocumentQuery, ApplicationResult<SitemapDocumentResult>>>();
        Mock<IQueryHandler<GetPublicHtmlSitemapNodesQuery, ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>>> htmlSitemapNodesHandler = new Mock<IQueryHandler<GetPublicHtmlSitemapNodesQuery, ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>>>();

        return new SeoController(
            Options.Create(settings),
            environment.Object,
            sitemapSettingsRepository.Object,
            sitemapDocumentHandler.Object,
            htmlSitemapNodesHandler.Object);
    }
}
