using AmusementPark.Application.Common.Results;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class PaginationHttpMapperTests
{
    [Fact]
    public void ToApplication_WhenRequestProvided_ShouldMapPageAndSize()
    {
        PaginationRequestDto request = new PaginationRequestDto { Page = 3, Size = 25 };

        AmusementPark.Application.Common.Requests.PagedQuery result = request.ToApplication();

        Assert.Equal(3, result.Page);
        Assert.Equal(25, result.PageSize);
    }

    [Fact]
    public void ToApplication_WhenRequestIsNull_ShouldThrow()
    {
        PaginationRequestDto? request = null;

        Assert.Throws<ArgumentNullException>(() => request!.ToApplication());
    }

    [Fact]
    public void Override_WhenValuesProvided_ShouldOverrideOnlyProvidedValues()
    {
        PaginationRequestDto request = new PaginationRequestDto { Page = 3, Size = 25 };

        PaginationRequestDto result = request.Override(size: 10);

        Assert.Equal(3, result.Page);
        Assert.Equal(10, result.Size);
    }

    [Fact]
    public void ToHttp_WhenPagedResultProvided_ShouldMapPagination()
    {
        PagedResult<string> source = new PagedResult<string>(new[] { "a" }, 2, 10, 25);

        PaginationDto result = source.ToHttp();

        Assert.Equal(25, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(10, result.ItemsPerPage);
    }

    [Fact]
    public void ToPagedResponse_WhenPagedResultProvided_ShouldMapItemsAndPagination()
    {
        PagedResult<int> source = new PagedResult<int>(new[] { 1, 2 }, 1, 2, 5);

        PagedResponseDto<string> result = source.ToPagedResponse(static item => $"#{item}");

        Assert.Equal(new[] { "#1", "#2" }, result.Data);
        Assert.Equal(5, result.Pagination.TotalItems);
    }

    [Fact]
    public void ToPagedResponse_WhenRequestAndItemsProvided_ShouldApplyInMemoryPagination()
    {
        PaginationRequestDto request = new PaginationRequestDto { Page = 2, Size = 2 };
        Int32[] items = new[] { 1, 2, 3, 4, 5 };

        PagedResponseDto<string> result = request.ToPagedResponse(items, static item => $"#{item}");

        Assert.Equal(new[] { "#3", "#4" }, result.Data);
        Assert.Equal(5, result.Pagination.TotalItems);
        Assert.Equal(3, result.Pagination.TotalPages);
    }
}
