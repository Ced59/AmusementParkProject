using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using Xunit;

namespace AmusementPark.Application.Tests.Features.AttractionAccessConditionTypes;

public sealed class AttractionAccessConditionTypeDefaultCatalogTests
{
    [Fact]
    public void BuildSystemDefinitions_WhenCalled_ShouldReturnExpectedSystemDefinitions()
    {
        IReadOnlyCollection<AttractionAccessConditionTypeDefinitionWriteModel> definitions = AttractionAccessConditionTypeDefaultCatalog.BuildSystemDefinitions();

        Assert.Equal(10, definitions.Count);
        Assert.All(definitions, static definition => Assert.True(definition.IsSystem));
        Assert.All(definitions, static definition => Assert.True(definition.IsActive));
        Assert.Equal(definitions.Count, definitions.Select(static definition => definition.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(definitions.Select(static definition => definition.SortOrder).OrderBy(static sortOrder => sortOrder), definitions.Select(static definition => definition.SortOrder));
    }

    [Fact]
    public void BuildSystemDefinitions_WhenCalled_ShouldProvideLabelsForAllSupportedLanguages()
    {
        IReadOnlyCollection<AttractionAccessConditionTypeDefinitionWriteModel> definitions = AttractionAccessConditionTypeDefaultCatalog.BuildSystemDefinitions();
        string[] expectedLanguages = new[] { "fr", "en", "es", "de", "it", "pl", "nl", "pt" };

        foreach (AttractionAccessConditionTypeDefinitionWriteModel definition in definitions)
        {
            Assert.Equal(expectedLanguages.OrderBy(static language => language), definition.Labels.Select(static label => label.LanguageCode).OrderBy(static language => language));
            Assert.All(definition.Labels, static label => Assert.False(string.IsNullOrWhiteSpace(label.Value)));
        }
    }

    [Fact]
    public void FallbackLabels_WhenRawNameProvided_ShouldUseTrimmedRawNameForFrenchAndEnglish()
    {
        IReadOnlyCollection<LocalizedTextValue> labels = AttractionAccessConditionTypeDefaultCatalog.FallbackLabels("custom-key", "  Custom label  ");

        Assert.Equal(2, labels.Count);
        Assert.Contains(labels, static label => label.LanguageCode == "fr" && label.Value == "Custom label");
        Assert.Contains(labels, static label => label.LanguageCode == "en" && label.Value == "Custom label");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FallbackLabels_WhenRawNameIsBlank_ShouldUseKeyAsFallback(string? rawName)
    {
        IReadOnlyCollection<LocalizedTextValue> labels = AttractionAccessConditionTypeDefaultCatalog.FallbackLabels("custom-key", rawName);

        Assert.All(labels, static label => Assert.Equal("custom-key", label.Value));
    }
}
