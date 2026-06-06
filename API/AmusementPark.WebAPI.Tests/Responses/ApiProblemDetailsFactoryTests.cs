using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Responses;

public sealed class ApiProblemDetailsFactoryTests
{
    [Fact]
    public void Create_WhenValuesProvided_ShouldBuildProblemDetailsWithTraceAndErrorCode()
    {
        DefaultHttpContext context = CreateContext("/api/parks");

        ProblemDetails result = ApiProblemDetailsFactory.Create(context, StatusCodes.Status404NotFound, "Not found", "Missing", "park.not-found");

        Assert.Equal("https://amusement-parks.fun/problems/not-found", result.Type);
        Assert.Equal("Not found", result.Title);
        Assert.Equal(StatusCodes.Status404NotFound, result.Status);
        Assert.Equal("Missing", result.Detail);
        Assert.Equal("/api/parks", result.Instance);
        Assert.Equal("trace-123", result.Extensions["traceId"]);
        Assert.Equal("park.not-found", result.Extensions["errorCode"]);
    }

    [Fact]
    public void CreateValidation_WhenModelStateContainsBlankErrorMessage_ShouldUseDefaultInvalidMessage()
    {
        DefaultHttpContext context = CreateContext("/api/parks");
        ModelStateDictionary modelState = new ModelStateDictionary();
        modelState.AddModelError("name", string.Empty);

        ValidationProblemDetails result = ApiProblemDetailsFactory.CreateValidation(context, modelState);

        Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
        Assert.Equal("validation.model-state.invalid", result.Extensions["errorCode"]);
        Assert.Equal("The submitted value is invalid.", Assert.Single(result.Errors["name"]));
    }

    [Fact]
    public void CreateValidation_WhenErrorsProvided_ShouldNormalizeErrorsAndUseProvidedDetail()
    {
        DefaultHttpContext context = CreateContext("/api/parks");
        Dictionary<string, IReadOnlyCollection<string>> errors = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["Name"] = new[] { "required" },
        };

        ValidationProblemDetails result = ApiProblemDetailsFactory.CreateValidation(context, errors, "validation.required", "Custom detail");

        Assert.Equal("Validation failed.", result.Title);
        Assert.Equal("Custom detail", result.Detail);
        Assert.Equal("validation.required", result.Extensions["errorCode"]);
        Assert.Equal(new[] { "required" }, result.Errors["Name"]);
    }

    [Fact]
    public void ToObjectResult_WhenProblemDetailsProvided_ShouldUseProblemJsonContentTypeAndStatusCode()
    {
        ProblemDetails problemDetails = new ProblemDetails { Status = StatusCodes.Status409Conflict };

        ObjectResult result = ApiProblemDetailsFactory.ToObjectResult(problemDetails);

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Contains(ApiProblemDetailsFactory.ProblemJsonContentType, result.ContentTypes);
        Assert.Same(problemDetails, result.Value);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, "Bad request.")]
    [InlineData(StatusCodes.Status401Unauthorized, "Authentication is required.")]
    [InlineData(StatusCodes.Status403Forbidden, "Access is forbidden.")]
    [InlineData(StatusCodes.Status404NotFound, "Resource not found.")]
    [InlineData(StatusCodes.Status409Conflict, "Conflict.")]
    [InlineData(599, "HTTP error.")]
    public void GetDefaultTitle_WhenStatusProvided_ShouldReturnExpectedTitle(int statusCode, string expected)
    {
        string result = ApiProblemDetailsFactory.GetDefaultTitle(statusCode);

        Assert.Equal(expected, result);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Path = path;
        context.TraceIdentifier = "trace-123";
        return context;
    }
}
