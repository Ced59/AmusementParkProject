using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertTextEncodingTests
{
    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenPlainAndRichTextsContainEntities_ShouldReturnExactPaths()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "history": {
            "events": [
              {
                "titles": [
                  { "languageCode": "fr", "value": "L&rsquo;ancien parc" }
                ],
                "summaries": [
                  { "languageCode": "fr", "value": "L&#8217;État annonce la rénovation." }
                ]
              }
            ]
          },
          "park": {
            "descriptions": [
              { "languageCode": "fr", "value": "<p>Un caf&eacute; borde la place.</p>" }
            ]
          }
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, static error => error.StartsWith("$.history.events[0].titles[0].value ", StringComparison.Ordinal));
        Assert.Contains(errors, static error => error.StartsWith("$.history.events[0].summaries[0].value ", StringComparison.Ordinal));
        Assert.Contains(errors, static error => error.StartsWith("$.park.descriptions[0].value ", StringComparison.Ordinal));
    }

    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenTextUsesUnicodeAndStructuralEscaping_ShouldReturnNoError()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "park": {
            "name": "L’ancien parc",
            "descriptions": [
              { "languageCode": "fr", "value": "<p>Rock &amp; Roll, avec 2 &lt; 3 et 4 &gt; 3.</p>" }
            ]
          }
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        Assert.Empty(errors);
    }

    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenPlainTextUsesStructuralEntity_ShouldReturnExactPath()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "park": {
            "name": "Rock &amp; Roll Plaza"
          }
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        string error = Assert.Single(errors);
        Assert.StartsWith("$.park.name ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenTechnicalUrlUsesEncodedQuerySeparator_ShouldReturnNoError()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "park": {
            "websiteUrl": "https://example.test/search?kind=park&amp;year=2026",
            "sourceUrl": "https://example.test/source?lang=fr&amp;view=full"
          }
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        Assert.Empty(errors);
    }

    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenIdentitySelectorContainsEntity_ShouldIgnoreSelectorAndValidatePersistedMapFileName()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "identity": {
            "parkId": "park-1",
            "name": "Legacy &amp; Park"
          },
          "park": {
            "name": "Legacy & Park"
          },
          "officialMaps": [
            {
              "originalFileName": "Plan &amp; guide.pdf"
            }
          ]
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        string error = Assert.Single(errors);
        Assert.StartsWith("$.officialMaps[0].originalFileName ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenOperationalNotesContainEntities_ShouldOnlyFlagPublicPricingNotes()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "metadata": {
            "notes": "Source A &amp; source B"
          },
          "openingHours": {
            "notes": "Internal A &amp; internal B"
          },
          "pricing": {
            "notes": [
              { "languageCode": "en", "value": "Admission &amp; parking" }
            ]
          }
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        string error = Assert.Single(errors);
        Assert.StartsWith("$.pricing.notes[0].value ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenArticleBlockUsesStructuralEntity_ShouldReturnExactPath()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "history": {
            "events": [
              {
                "article": {
                  "blocks": [
                    {
                      "texts": [
                        { "languageCode": "en", "value": "Rock &amp; Roll" }
                      ]
                    }
                  ]
                }
              }
            ]
          }
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        string error = Assert.Single(errors);
        Assert.StartsWith("$.history.events[0].article.blocks[0].texts[0].value ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenHistoryUsesCompactLocalizedShapes_ShouldReturnEveryExactPath()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "history": {
            "events": [
              {
                "summary": { "fr": "L&rsquo;histoire" },
                "article": {
                  "subtitle": "Rock &amp; Roll",
                  "blocks": [
                    {
                      "text": "Ouverture &agrave; midi",
                      "captions": { "en": "Then &amp; now" }
                    }
                  ]
                }
              }
            ]
          }
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        Assert.Equal(4, errors.Count);
        Assert.Contains(errors, static error => error.StartsWith("$.history.events[0].summary.fr ", StringComparison.Ordinal));
        Assert.Contains(errors, static error => error.StartsWith("$.history.events[0].article.subtitle ", StringComparison.Ordinal));
        Assert.Contains(errors, static error => error.StartsWith("$.history.events[0].article.blocks[0].text ", StringComparison.Ordinal));
        Assert.Contains(errors, static error => error.StartsWith("$.history.events[0].article.blocks[0].captions.en ", StringComparison.Ordinal));
    }

    [Fact]
    public void FindEncodedDisplayEntityErrors_WhenHistoryEventsAliasUsesCompactLocalizedShape_ShouldReturnExactPath()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "historyEvents": [
            {
              "summary": { "fr": "L&rsquo;histoire" }
            }
          ]
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        string error = Assert.Single(errors);
        Assert.StartsWith("$.historyEvents[0].summary.fr ", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<p>L&amp;rsquo;ancien parc</p>")]
    [InlineData("<p>L&amp;#8217;ancien parc</p>")]
    [InlineData("<p>Rock &amp;amp; Roll</p>")]
    public void FindEncodedDisplayEntityErrors_WhenRichTextIsDoubleEncoded_ShouldReturnError(string value)
    {
        using JsonDocument document = JsonDocument.Parse($$"""
        {
          "park": {
            "descriptions": [
              { "languageCode": "fr", "value": "{{value}}" }
            ]
          }
        }
        """);

        IReadOnlyCollection<string> errors = ParkGraphUpsertTextEncodingValidator.FindErrors(document.RootElement);

        Assert.Single(errors);
    }
}
