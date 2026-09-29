using AmusementPark.WebAPI.Diagnostics;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Diagnostics;

public sealed class ApiPerformanceOutcomeClassifierTests
{
    [Theory]
    [InlineData(200, ApiPerformanceOutcome.Success, "2xx")]
    [InlineData(204, ApiPerformanceOutcome.Success, "2xx")]
    [InlineData(302, ApiPerformanceOutcome.Redirection, "3xx")]
    [InlineData(404, ApiPerformanceOutcome.ClientError, "4xx")]
    [InlineData(429, ApiPerformanceOutcome.ClientError, "4xx")]
    [InlineData(500, ApiPerformanceOutcome.ServerError, "5xx")]
    [InlineData(503, ApiPerformanceOutcome.ServerError, "5xx")]
    public void Classify_ShouldReturnStableOutcomeAndStatusFamily(
        int statusCode,
        ApiPerformanceOutcome expectedOutcome,
        string expectedStatusFamily)
    {
        ApiPerformanceOutcome outcome = ApiPerformanceOutcomeClassifier.Classify(statusCode);
        string statusFamily = ApiPerformanceOutcomeClassifier.GetStatusFamily(statusCode);

        Assert.Equal(expectedOutcome, outcome);
        Assert.Equal(expectedStatusFamily, statusFamily);
    }

    [Theory]
    [InlineData(0, "1xx")]
    [InlineData(999, "5xx")]
    public void GetStatusFamily_ShouldBoundUnexpectedValues(int statusCode, string expected)
    {
        string result = ApiPerformanceOutcomeClassifier.GetStatusFamily(statusCode);

        Assert.Equal(expected, result);
    }
}
