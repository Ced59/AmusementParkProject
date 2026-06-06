using AmusementPark.Application.Errors;
using Xunit;

namespace AmusementPark.Application.Tests.Errors;

public sealed class ApplicationErrorTests
{
    [Fact]
    public void Validation_WhenDetailsProvided_ShouldKeepCodeMessageTypeAndDetails()
    {
        Dictionary<string, IReadOnlyCollection<string>> details = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["name"] = new[] { "required" },
        };

        ApplicationError error = ApplicationError.Validation("code", "message", details);

        Assert.Equal("code", error.Code);
        Assert.Equal("message", error.Message);
        Assert.Equal(ApplicationErrorType.Validation, error.Type);
        Assert.Same(details, error.Details);
    }

    [Theory]
    [InlineData(ApplicationErrorType.NotFound, "not-found")]
    [InlineData(ApplicationErrorType.RuleViolation, "rule")]
    [InlineData(ApplicationErrorType.Conflict, "conflict")]
    [InlineData(ApplicationErrorType.Unauthorized, "unauthorized")]
    [InlineData(ApplicationErrorType.Forbidden, "forbidden")]
    [InlineData(ApplicationErrorType.Technical, "technical")]
    public void FactoryMethods_WhenCalled_ShouldSetExpectedErrorType(ApplicationErrorType expectedType, string factoryName)
    {
        ApplicationError error = factoryName switch
        {
            "not-found" => ApplicationError.NotFound("code", "message"),
            "rule" => ApplicationError.RuleViolation("code", "message"),
            "conflict" => ApplicationError.Conflict("code", "message"),
            "unauthorized" => ApplicationError.Unauthorized("code", "message"),
            "forbidden" => ApplicationError.Forbidden("code", "message"),
            "technical" => ApplicationError.Technical("code", "message"),
            _ => throw new InvalidOperationException("Unknown factory."),
        };

        Assert.Equal(expectedType, error.Type);
        Assert.Equal("code", error.Code);
        Assert.Equal("message", error.Message);
        Assert.Null(error.Details);
    }
}
