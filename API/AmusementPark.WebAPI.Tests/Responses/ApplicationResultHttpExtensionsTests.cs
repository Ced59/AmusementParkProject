using AmusementPark.Application.Errors;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Responses;

public sealed class ApplicationResultHttpExtensionsTests
{
    [Fact]
    public void ToActionResult_WhenGenericResultIsSuccess_ShouldReturnOkWithValue()
    {
        TestController controller = CreateController("/parks");
        ApplicationResult<string> result = ApplicationResult<string>.Success("ok");

        IActionResult actionResult = controller.ToActionResult(result);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("ok", ok.Value);
    }

    [Fact]
    public void ToActionResult_WhenNonGenericResultIsSuccess_ShouldReturnOk()
    {
        TestController controller = CreateController("/parks");
        ApplicationResult result = ApplicationResult.Success();

        IActionResult actionResult = controller.ToActionResult(result);

        Assert.IsType<OkResult>(actionResult);
    }

    [Fact]
    public void ToActionResult_WhenValidationResultHasDetails_ShouldReturnValidationProblemDetails()
    {
        TestController controller = CreateController("/parks");
        Dictionary<string, IReadOnlyCollection<string>> details = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["name"] = new[] { "required" },
        };
        ApplicationResult result = ApplicationResult.Failure(ApplicationError.Validation("validation.required", "Name required", details));

        IActionResult actionResult = controller.ToActionResult(result);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ValidationProblemDetails problemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("validation.required", problemDetails.Extensions["errorCode"]);
        Assert.Equal(new[] { "required" }, problemDetails.Errors["name"]);
    }

    [Fact]
    public void ToActionResult_WhenTechnicalError_ShouldHideInternalDetail()
    {
        TestController controller = CreateController("/parks");
        ApplicationResult result = ApplicationResult.Failure(ApplicationError.Technical("technical.failure", "Database connection string leaked here."));

        IActionResult actionResult = controller.ToActionResult(result);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ProblemDetails problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("Unexpected server error.", problemDetails.Title);
        Assert.Equal("An unexpected error occurred.", problemDetails.Detail);
        Assert.DoesNotContain("Database connection", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToActionResult_WhenNotFoundError_ShouldReturnProblemDetailsWithOriginalMessage()
    {
        TestController controller = CreateController("/parks");
        ApplicationResult result = ApplicationResult.Failure(ApplicationError.NotFound("park.not-found", "Park missing"));

        IActionResult actionResult = controller.ToActionResult(result);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ProblemDetails problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Park missing", problemDetails.Detail);
        Assert.Equal("park.not-found", problemDetails.Extensions["errorCode"]);
    }

    private static TestController CreateController(string path)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.TraceIdentifier = "trace-123";
        return new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            },
        };
    }

    private sealed class TestController : ControllerBase
    {
    }
}
