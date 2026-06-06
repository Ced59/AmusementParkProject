using AmusementPark.Application.Common.Results;
using Xunit;

namespace AmusementPark.Application.Tests.Common.Results;

public sealed class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void TotalPages_WhenPageSizeIsPositive_ShouldRoundUp(long totalItems, int pageSize, int expectedTotalPages)
    {
        PagedResult<string> result = new PagedResult<string>(Array.Empty<string>(), 1, pageSize, totalItems);

        Assert.Equal(expectedTotalPages, result.TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TotalPages_WhenPageSizeIsZeroOrNegative_ShouldReturnZero(int pageSize)
    {
        PagedResult<string> result = new PagedResult<string>(Array.Empty<string>(), 1, pageSize, 100);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Constructor_WhenItemsProvided_ShouldExposeAllPaginationValues()
    {
        string[] items = new[] { "a", "b" };

        PagedResult<string> result = new PagedResult<string>(items, 2, 5, 12);

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(12, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
    }
}
