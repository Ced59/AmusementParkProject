using AmusementPark.WebAPI.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmusementPark.WebAPI.Tests.DependencyInjection;

public sealed class PassportExportRateLimitingTests
{
    [Theory]
    [InlineData("?download=true", true)]
    [InlineData("?download=false", false)]
    [InlineData("", false)]
    public void IsPassportExportDownload_LimitsOnlyArtifactTransfers(
        string queryString,
        bool expected)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Path = "/me/passport/exports/export-1";
        context.Request.QueryString = new QueryString(queryString);

        bool result = RateLimitingServiceCollectionExtensions
            .IsPassportExportDownload(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPassportExportDownload_DoesNotLimitUnrelatedQueryParameters()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Path = "/parks/export-1";
        context.Request.QueryString = new QueryString("?download=true");

        bool result = RateLimitingServiceCollectionExtensions
            .IsPassportExportDownload(context);

        Assert.False(result);
    }
}
