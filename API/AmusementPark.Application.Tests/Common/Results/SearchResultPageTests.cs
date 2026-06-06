using AmusementPark.Application.Common.Results;
using Xunit;

namespace AmusementPark.Application.Tests.Common.Results;

public sealed class SearchResultPageTests
{
    [Fact]
    public void Constructor_WhenValuesProvided_ShouldBehaveAsPagedResult()
    {
        string[] items = new[] { "walibi", "efteling" };

        SearchResultPage<string> result = new SearchResultPage<string>(items, 2, 1, 2);

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
    }
}
