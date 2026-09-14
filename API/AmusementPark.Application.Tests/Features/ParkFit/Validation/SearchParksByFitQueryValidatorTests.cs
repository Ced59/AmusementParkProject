using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Validation;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Validation;

public sealed class SearchParksByFitQueryValidatorTests
{
    private readonly SearchParksByFitQueryValidator validator =
        new SearchParksByFitQueryValidator();

    [Fact]
    public void Validate_WhenSearchIsBoundedAndAnonymous_ShouldAcceptIt()
    {
        IReadOnlyCollection<ApplicationError> result = this.validator.Validate(BuildValidQuery());

        Assert.Empty(result);
    }

    [Fact]
    public void Validate_WhenEvaluationDateIsMissing_ShouldRejectIt()
    {
        SearchParksByFitQuery query = BuildValidQuery() with
        {
            EvaluationDate = default,
        };

        IReadOnlyCollection<ApplicationError> result = this.validator.Validate(query);

        Assert.Contains(result, error => error.Details?.ContainsKey("EvaluationDate") == true);
    }

    [Fact]
    public void Validate_WhenMemberKeyCouldContainPersonalText_ShouldRejectIt()
    {
        SearchParksByFitQuery query = BuildValidQuery() with
        {
            Members = new[] { BuildMember("Alice Dupont") },
        };

        IReadOnlyCollection<ApplicationError> result = this.validator.Validate(query);

        Assert.Contains(result, error => error.Details?.ContainsKey("MemberKey") == true);
    }

    [Fact]
    public void Validate_WhenMemberKeysAreDuplicated_ShouldRejectThem()
    {
        SearchParksByFitQuery query = BuildValidQuery() with
        {
            Members = new[] { BuildMember("member-1"), BuildMember("member-1") },
        };

        IReadOnlyCollection<ApplicationError> result = this.validator.Validate(query);

        Assert.Contains(result, error => error.Details?.ContainsKey("MemberKey") == true);
    }

    [Fact]
    public void Validate_WhenSearchExceedsPublicLimits_ShouldRejectIt()
    {
        SearchParksByFitQuery query = BuildValidQuery() with
        {
            Members = Enumerable.Range(1, ParkFitSearchLimits.MaximumMemberCount + 1)
                .Select(index => BuildMember($"member-{index}"))
                .ToList(),
            MaximumResults = ParkFitSearchLimits.MaximumResultCount + 1,
        };

        IReadOnlyCollection<ApplicationError> result = this.validator.Validate(query);

        Assert.Contains(result, error => error.Details?.ContainsKey("Members") == true);
        Assert.Contains(result, error => error.Details?.ContainsKey("MaximumResults") == true);
    }

    [Fact]
    public void Validate_WhenCompanionAgeHasNoConfirmedCompanion_ShouldRejectIt()
    {
        SearchParksByFitQuery query = BuildValidQuery() with
        {
            Members = new[]
            {
                new ParkFitSearchMemberCriteria(
                    "member-1",
                    120,
                    8,
                    8,
                    false,
                    18,
                    70),
            },
        };

        IReadOnlyCollection<ApplicationError> result = this.validator.Validate(query);

        Assert.Contains(result, error => error.Details?.ContainsKey("CanBeAccompanied") == true);
    }

    [Theory]
    [InlineData(91d, 2d)]
    [InlineData(45d, 181d)]
    [InlineData(45d, null)]
    public void Validate_WhenOriginIsIncompleteOrOutOfRange_ShouldRejectIt(
        double? latitude,
        double? longitude)
    {
        SearchParksByFitQuery query = BuildValidQuery() with
        {
            OriginLatitude = latitude,
            OriginLongitude = longitude,
        };

        IReadOnlyCollection<ApplicationError> result = this.validator.Validate(query);

        Assert.Contains(result, error => error.Details?.ContainsKey("OriginLatitude") == true);
    }

    [Fact]
    public void Validate_WhenOriginIsCompleteAndValid_ShouldAcceptIt()
    {
        SearchParksByFitQuery query = BuildValidQuery() with
        {
            OriginLatitude = 50.6292d,
            OriginLongitude = 3.0573d,
        };

        IReadOnlyCollection<ApplicationError> result = this.validator.Validate(query);

        Assert.Empty(result);
    }

    private static SearchParksByFitQuery BuildValidQuery()
    {
        return new SearchParksByFitQuery(
            new DateOnly(2026, 10, 10),
            new[] { BuildMember("member-1") },
            new[] { ParkItemType.FamilyRide },
            true,
            "FR",
            ParkFitUnknownDataPolicy.KeepWithWarning,
            10);
    }

    private static ParkFitSearchMemberCriteria BuildMember(string key)
    {
        return new ParkFitSearchMemberCriteria(
            key,
            120,
            8,
            8,
            true,
            18,
            70);
    }
}
