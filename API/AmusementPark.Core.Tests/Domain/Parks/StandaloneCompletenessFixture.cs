using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Tests.Domain.Parks;

internal sealed class StandaloneCompletenessFixture
{
    internal static readonly DateTime AsOf = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    internal StandaloneCompletenessFixture()
    {
        this.Attraction.SetPosition(37, -8);
    }

    internal StandaloneAttraction Attraction { get; } = new()
    {
        Id = "standalone-1", Name = "Cliff Flight", CountryCode = "PT", City = "Faro",
        Type = ParkItemType.Attraction, Subtype = "Flight experience", OperatorId = "operator-1",
        WebsiteUrl = "https://example.test/flight",
        Descriptions = Texts("<h2>The chalk cliffs</h2><p>The white headland rises above a quiet inlet where fishing boats gather.</p>"),
        AttractionDetails = new()
        {
            Status = ParkItemStatusNormalizer.Operating, OpeningDateText = "2019",
            SourceUrl = "https://example.test/about", ManufacturerId = "manufacturer-1",
            AccessConditions = [new()
            {
                Type = AttractionAccessConditionType.MinAge, Value = 4,
                Label = Texts("Four years and over"), SourceSummary = Texts("Minimum age: four"),
                SourceKind = AttractionAccessConditionSourceKind.Official,
                SourceConfidence = AttractionAccessConditionConfidence.High,
                SourceUrl = "https://example.test/access", VerifiedAtUtc = AsOf,
            }],
        },
    };

    internal StandaloneAttractionDataCompletenessContext Context { get; set; } = new()
    {
        ProjectForPublication = true, AsOfUtc = AsOf,
        Operator = new()
        {
            Id = "operator-1", Name = "Coastal Leisure",
            Description = Texts("Founded by local guides, Coastal Leisure also runs walking tours through the wetlands."),
        },
        Manufacturer = new()
        {
            Id = "manufacturer-1", Name = "North Workshop", IsVisible = false,
            Biography = Texts("North Workshop began supplying touring theatre companies before entering the leisure industry."),
        },
        Images = [new()
        {
            Id = "photo-1", OwnerType = ImageOwnerType.StandaloneAttraction, OwnerId = "standalone-1",
            IsCurrent = true, IsPublished = false, SourceUrl = "https://example.test/photo.jpg", Credits = Texts("Coastal Leisure"),
            AltTexts = Texts("The white cliff above the bay"), Captions = Texts("Fishing boats shelter beneath the headland"),
        }],
        HistoryEvents = [new()
        {
            Id = "event-1", OwnerId = "standalone-1", EntityType = HistoryEntityType.StandaloneAttraction,
            IsVisible = false, Year = 2019, Titles = Texts("Opening"),
            Summaries = Texts("The coastal attraction welcomed its first guests during the spring festival."),
            Sources = [new() { Url = "https://example.test/history" }], MainImageId = "photo-1",
            Article = new()
            {
                IsPublished = false, Titles = Texts("The first season"),
                Summaries = Texts("A former orchard became a meeting place for the village's summer celebrations."),
                Sources = [new() { Url = "https://example.test/opening" }], MainImageId = "photo-1",
                Blocks = [
                    new() { Type = HistoryArticleBlockType.Heading, Texts = Texts("The orchard") },
                    new() { Type = HistoryArticleBlockType.Paragraph, Texts = Texts("Stone walls still marked the old farm boundary when construction began in autumn.") },
                    new() { Type = HistoryArticleBlockType.Paragraph, Texts = Texts("Neighbours donated photographs of the harvest for an exhibition inside the entrance building.") },
                    new() { Type = HistoryArticleBlockType.Heading, Texts = Texts("Summer evenings") },
                    new() { Type = HistoryArticleBlockType.Paragraph, Texts = Texts("Evening concerts brought musicians from several coastal villages to the new courtyard.") },
                ],
            },
        }],
        Pricing = new()
        {
            ParkId = "standalone-1", CurrencyCode = "EUR", SourceUrl = "https://example.test/prices", LastVerifiedAtUtc = AsOf,
            AdmissionOffers = [new() { Labels = Texts("Adult"), OnlinePrice = new() { Amount = 20 } }],
        },
        OpeningHours = new()
        {
            ParkId = "standalone-1", TimeZoneId = "Europe/Lisbon", SourceUrl = "https://example.test/hours", LastVerifiedAtUtc = AsOf,
            RegularRules = [new()
            {
                StartDate = new(2026, 10, 1), EndDate = new(2026, 10, 31), DaysOfWeek = [DayOfWeek.Friday],
                TimeRanges = [new() { OpensAt = new(10, 0), ClosesAt = new(18, 0) }],
            }],
        },
    };

    internal static List<LocalizedText> Texts(string value) =>
        new[] { "fr", "en", "de", "nl", "it", "es", "pl", "pt" }.Select(language => new LocalizedText(language, value)).ToList();
}
