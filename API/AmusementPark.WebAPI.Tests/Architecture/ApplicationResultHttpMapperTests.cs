using AmusementPark.Application.Errors;
using AmusementPark.WebAPI.Architecture;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Architecture;

public sealed class ApplicationResultHttpMapperTests
{
    [Theory]
    [InlineData(ApplicationErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ApplicationErrorType.RuleViolation, StatusCodes.Status400BadRequest)]
    [InlineData(ApplicationErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ApplicationErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ApplicationErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ApplicationErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ApplicationErrorType.Technical, StatusCodes.Status500InternalServerError)]
    public void ToStatusCode_WhenErrorTypeProvided_ShouldReturnExpectedHttpStatus(ApplicationErrorType errorType, int expectedStatusCode)
    {
        int result = ApplicationResultHttpMapper.ToStatusCode(errorType);

        Assert.Equal(expectedStatusCode, result);
    }
}
