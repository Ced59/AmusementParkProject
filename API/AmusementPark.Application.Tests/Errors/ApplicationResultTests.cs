using AmusementPark.Application.Errors;
using Xunit;

namespace AmusementPark.Application.Tests.Errors;

public sealed class ApplicationResultTests
{
    [Fact]
    public void Success_WhenNonGeneric_ShouldContainNoErrorsAndBeSuccessful()
    {
        ApplicationResult result = ApplicationResult.Success();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WhenErrorsArrayProvided_ShouldExposeErrorsAndBeFailure()
    {
        ApplicationError error = ApplicationError.Technical("technical", "message");

        ApplicationResult result = ApplicationResult.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(new[] { error }, result.Errors);
    }

    [Fact]
    public void Failure_WhenErrorsArrayIsNull_ShouldThrow()
    {
        ApplicationError[]? errors = null;

        Assert.Throws<ArgumentNullException>(() => ApplicationResult.Failure(errors!));
    }

    [Fact]
    public void Success_WhenGeneric_ShouldExposeValueAndNoErrors()
    {
        ApplicationResult<string> result = ApplicationResult<string>.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WhenGeneric_ShouldExposeDefaultValueAndErrors()
    {
        ApplicationError error = ApplicationError.NotFound("missing", "Missing");

        ApplicationResult<string> result = ApplicationResult<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(new[] { error }, result.Errors);
    }

    [Fact]
    public void Failure_WhenGenericErrorsCollectionIsNull_ShouldThrow()
    {
        IReadOnlyCollection<ApplicationError>? errors = null;

        Assert.Throws<ArgumentNullException>(() => ApplicationResult<int>.Failure(errors!));
    }
}
