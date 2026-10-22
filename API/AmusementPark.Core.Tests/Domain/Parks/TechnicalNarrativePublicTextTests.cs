using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class TechnicalNarrativePublicTextTests
{
    [Theory]
    [InlineData("Le tracé, les sièges et les accélérations déterminent le mouvement.")]
    [InlineData("The track, seats and acceleration determine the movement.")]
    [InlineData("Die Strecke, Sitze und Beschleunigungen bestimmen die Bewegung.")]
    [InlineData("De baan, zitplaatsen en versnellingen bepalen de beweging.")]
    [InlineData("Il tracciato, i sedili e le accelerazioni determinano il movimento.")]
    [InlineData("El trazado, los asientos y las aceleraciones determinan el movimiento.")]
    [InlineData("Tory, siedzenia i obroty określają ruch.")]
    [InlineData("O percurso, os assentos e as acelerações determinam o movimento.")]
    public void ThreeTechnicalCategories_StillBlockEveryPublicLanguage(string value)
    {
        Assert.True(DataCompletenessScoringRules.HasForbiddenStructuredLabelPublicText(value));
        Assert.True(DataCompletenessScoringRules.HasForbiddenPlainPublicText(value));
        Assert.True(DataCompletenessScoringRules.HasForbiddenRichPublicText($"<p>{value}</p>"));
    }

    [Theory]
    [InlineData("TRACK / SEATS / SPEED", true)]
    [InlineData("Track, rails, layout and structure", false)]
    [InlineData("Track and seats", false)]
    [InlineData("Track and acceleration", false)]
    [InlineData("Tracking seating speedboat", false)]
    [InlineData("A lift hill above the seats beside the track", true)]
    [InlineData("Minimum height: 120 cm", false)]
    public void CategoryCounting_PreservesBoundariesCaseAndStructuredMetrics(string value, bool expected)
    {
        Assert.Equal(expected, DataCompletenessScoringRules.HasForbiddenStructuredLabelPublicText(value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongNearMatches_PreserveClassificationWithoutTimeout(bool appendTechnicalNarrative)
    {
        string value = string.Concat(Enumerable.Repeat("tracking seating speedboat rotationnel ", 500));
        if (appendTechnicalNarrative)
        {
            value += " TRACK SEATS SPEED";
        }

        Assert.Equal(appendTechnicalNarrative, DataCompletenessScoringRules.HasForbiddenStructuredLabelPublicText(value));
    }
}
