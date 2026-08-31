using System.Text.Json;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class VisitIdentifiersTests
{
    [Fact]
    public void VisitIdNew_ShouldGenerateANormalizedOpaqueString()
    {
        VisitId identifier = VisitId.New();

        Assert.Equal(32, identifier.Value.Length);
        Assert.True(Guid.TryParseExact(identifier.Value, "N", out Guid _));
        Assert.Equal(identifier.Value, identifier.ToString());
    }

    [Fact]
    public void RideOccurrenceIdNew_ShouldGenerateANormalizedOpaqueString()
    {
        RideOccurrenceId identifier = RideOccurrenceId.New();

        Assert.Equal(32, identifier.Value.Length);
        Assert.True(Guid.TryParseExact(identifier.Value, "N", out Guid _));
        Assert.Equal(identifier.Value, identifier.ToString());
    }

    [Fact]
    public void Parse_WhenValueUsesALegacyFormat_ShouldPreserveTheStorageValue()
    {
        VisitId visitId = VisitId.Parse("  legacy-Visit:42  ");
        RideOccurrenceId occurrenceId = RideOccurrenceId.Parse("  external/RIDE_7  ");

        Assert.Equal("legacy-Visit:42", visitId.Value);
        Assert.Equal("external/RIDE_7", occurrenceId.Value);
    }

    [Fact]
    public void Parse_WhenValuesDifferOnlyByCase_ShouldKeepThemDistinct()
    {
        VisitId upper = VisitId.Parse("Visit-1");
        VisitId lower = VisitId.Parse("visit-1");

        Assert.NotEqual(upper, lower);
    }

    [Fact]
    public void Parse_WhenValueIsInvalid_ShouldExposeTheStableDomainCode()
    {
        IdentifierValidationException exception = Assert.Throws<IdentifierValidationException>(
            () => VisitId.Parse(" "));

        Assert.Equal(IdentifierErrorCodes.Required, exception.ErrorCode);
    }

    [Fact]
    public void Value_WhenIdentifierIsUninitialized_ShouldRejectTheDefaultStruct()
    {
        VisitId identifier = default;

        Assert.Throws<InvalidOperationException>(() => identifier.Value);
    }

    [Fact]
    public void ContractMapping_ShouldKeepIdentifiersAsJsonStrings()
    {
        VisitIdentifierContract contract = new VisitIdentifierContract(
            VisitId.Parse("legacy-visit").Value,
            RideOccurrenceId.Parse("legacy-occurrence").Value);

        string json = JsonSerializer.Serialize(contract);
        VisitIdentifierContract? restored = JsonSerializer.Deserialize<VisitIdentifierContract>(json);

        Assert.NotNull(restored);
        Assert.Equal("legacy-visit", restored.VisitId);
        Assert.Equal("legacy-occurrence", restored.RideOccurrenceId);
        Assert.DoesNotContain("Value", json, StringComparison.Ordinal);
    }

    private sealed record VisitIdentifierContract(string VisitId, string RideOccurrenceId);
}
