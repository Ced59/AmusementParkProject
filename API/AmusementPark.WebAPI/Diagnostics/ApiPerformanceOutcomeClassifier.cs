using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Diagnostics;

public static class ApiPerformanceOutcomeClassifier
{
    public static ApiPerformanceOutcome Classify(int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            return ApiPerformanceOutcome.ServerError;
        }

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            return ApiPerformanceOutcome.ClientError;
        }

        if (statusCode >= StatusCodes.Status300MultipleChoices)
        {
            return ApiPerformanceOutcome.Redirection;
        }

        return ApiPerformanceOutcome.Success;
    }

    public static string GetStatusFamily(int statusCode)
    {
        int normalizedStatusCode = Math.Clamp(statusCode, 100, 599);
        return $"{normalizedStatusCode / 100}xx";
    }
}
