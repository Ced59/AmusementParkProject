using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Validation;
using Xunit;

namespace AmusementPark.Application.Tests.Validation;

public sealed class PagedQueryValidatorTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 50)]
    public void Validate_WhenPageAndPageSizeArePositive_ShouldReturnNoErrors(int page, int pageSize)
    {
        PagedQueryValidator validator = new PagedQueryValidator();

        IReadOnlyCollection<ApplicationError> errors = validator.Validate(new PagedQuery(page, pageSize));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Validate_WhenPageOrPageSizeIsInvalid_ShouldReturnInvalidPaginationError(int page, int pageSize)
    {
        PagedQueryValidator validator = new PagedQueryValidator();

        IReadOnlyCollection<ApplicationError> errors = validator.Validate(new PagedQuery(page, pageSize));

        ApplicationError error = Assert.Single(errors);
        Assert.Equal("validation.pagination.invalid", error.Code);
    }
}
