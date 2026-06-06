using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using Xunit;

namespace AmusementPark.Application.Tests.Features.AttractionAccessConditionTypes;

public sealed class AttractionAccessConditionTypeApplicationErrorsTests
{
    [Fact]
    public void InvalidKey_WhenCalled_ShouldReturnValidationError()
    {
        ApplicationError error = AttractionAccessConditionTypeApplicationErrors.InvalidKey();

        Assert.Equal("attraction-access-condition-type.key.invalid", error.Code);
        Assert.Equal(ApplicationErrorType.Validation, error.Type);
    }

    [Fact]
    public void MissingLabels_WhenCalled_ShouldReturnValidationError()
    {
        ApplicationError error = AttractionAccessConditionTypeApplicationErrors.MissingLabels();

        Assert.Equal("attraction-access-condition-type.labels.missing", error.Code);
        Assert.Equal(ApplicationErrorType.Validation, error.Type);
    }
}
