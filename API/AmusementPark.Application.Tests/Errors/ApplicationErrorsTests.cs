using AmusementPark.Application.Errors;
using Xunit;

namespace AmusementPark.Application.Tests.Errors;

public sealed class ApplicationErrorsTests
{
    [Fact]
    public void Required_WhenFieldNameProvided_ShouldBuildValidationErrorWithDetails()
    {
        ApplicationError error = ApplicationErrors.Required("Text");

        Assert.Equal("validation.required", error.Code);
        Assert.Equal(ApplicationErrorType.Validation, error.Type);
        Assert.Contains("Text", error.Message);
        Assert.NotNull(error.Details);
        Assert.True(error.Details.ContainsKey("Text"));
        Assert.Equal(new[] { "required" }, error.Details["Text"]);
    }

    [Fact]
    public void InvalidPagination_WhenCalled_ShouldBuildValidationErrorWithoutDetails()
    {
        ApplicationError error = ApplicationErrors.InvalidPagination();

        Assert.Equal("validation.pagination.invalid", error.Code);
        Assert.Equal(ApplicationErrorType.Validation, error.Type);
        Assert.Null(error.Details);
    }

    [Fact]
    public void EntityNotFound_WhenEntityNameIsMixedCase_ShouldLowercaseCodeOnly()
    {
        ApplicationError error = ApplicationErrors.EntityNotFound("ParkItem", "42");

        Assert.Equal("parkitem.not-found", error.Code);
        Assert.Contains("ParkItem", error.Message);
        Assert.Contains("42", error.Message);
        Assert.Equal(ApplicationErrorType.NotFound, error.Type);
    }

    [Fact]
    public void AlreadyExists_WhenEntityAndKeyProvided_ShouldBuildConflictError()
    {
        ApplicationError error = ApplicationErrors.AlreadyExists("Park", "walibi");

        Assert.Equal("park.already-exists", error.Code);
        Assert.Contains("walibi", error.Message);
        Assert.Equal(ApplicationErrorType.Conflict, error.Type);
    }
}
