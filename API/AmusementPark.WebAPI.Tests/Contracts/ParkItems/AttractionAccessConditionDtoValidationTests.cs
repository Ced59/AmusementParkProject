using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.ParkItems;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Contracts.ParkItems;

public sealed class AttractionAccessConditionDtoValidationTests
{
    [Fact]
    public void Validate_WhenContractVersionIsOmitted_ShouldRejectLegacyWrite()
    {
        AttractionAccessConditionDto dto = new AttractionAccessConditionDto
        {
            Type = AttractionAccessConditionTypeDto.MinHeight,
        };
        List<ValidationResult> results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

        Assert.False(isValid);
        Assert.Contains(results, static result =>
            result.MemberNames.Contains(nameof(AttractionAccessConditionDto.ProvenanceSchemaVersion)));
    }

    [Fact]
    public void Validate_WhenEvidenceTimestampHasNoTimezone_ShouldRejectAmbiguousInstant()
    {
        AttractionAccessConditionDto dto = new AttractionAccessConditionDto
        {
            Type = AttractionAccessConditionTypeDto.MinHeight,
            ProvenanceSchemaVersion = 1,
            CollectedAtUtc = new DateTime(2026, 8, 1, 8, 30, 0, DateTimeKind.Unspecified),
            VerifiedAtUtc = new DateTime(2026, 9, 1, 9, 45, 0, DateTimeKind.Unspecified),
        };
        List<ValidationResult> results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

        Assert.False(isValid);
        Assert.Contains(results, static result =>
            result.MemberNames.Contains(nameof(AttractionAccessConditionDto.CollectedAtUtc)));
        Assert.Contains(results, static result =>
            result.MemberNames.Contains(nameof(AttractionAccessConditionDto.VerifiedAtUtc)));
    }
}
