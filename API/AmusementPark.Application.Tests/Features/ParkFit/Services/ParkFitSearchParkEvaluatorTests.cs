using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Services;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Services;

public sealed class ParkFitSearchParkEvaluatorTests
{
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly EvaluationDate = new DateOnly(2026, 10, 10);

    [Theory]
    [InlineData(ParkFitDataQualityStatus.NotAssessed)]
    [InlineData(ParkFitDataQualityStatus.Insufficient)]
    [InlineData(ParkFitDataQualityStatus.EligibleForDiscoveryOnly)]
    [InlineData(ParkFitDataQualityStatus.TemporarilyStale)]
    [InlineData(ParkFitDataQualityStatus.Suspended)]
    public void Evaluate_WhenQualityGateIsNotEligible_ShouldRefuseToProduceAResult(
        ParkFitDataQualityStatus status)
    {
        ParkFitSearchParkEvaluator evaluator = new ParkFitSearchParkEvaluator();
        ParkFitDataQualityAssessment quality = new ParkFitDataQualityAssessment
        {
            ParkId = "park-1",
            ParkName = "Parc témoin",
            Status = status,
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(() => evaluator.Evaluate(
            new Park
            {
                Id = "park-1",
                Name = "Parc témoin",
            },
            Array.Empty<ParkItem>(),
            quality,
            null,
            Array.Empty<ParkFitEvaluatedMemberProfile>(),
            BuildQuery(),
            EvaluationTimestamp,
            0));

        Assert.Equal("quality", exception.ParamName);
    }

    private static SearchParksByFitQuery BuildQuery()
    {
        return new SearchParksByFitQuery(
            EvaluationDate,
            new[]
            {
                new ParkFitSearchMemberCriteria(
                    "member-1",
                    120,
                    8,
                    8,
                    false,
                    null,
                    null),
            },
            new[] { ParkItemType.FamilyRide },
            false,
            "fr",
            ParkFitUnknownDataPolicy.KeepWithWarning,
            10);
    }
}
