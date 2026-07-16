using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.History;

public enum HistoryEntityType
{
    Park = 0,
    ParkItem = 1,
    StandaloneAttraction = 2,
}

public enum HistoryDatePrecision
{
    Year = 0,
    Month = 1,
    Day = 2,
}

public enum ParkHistoryEventType
{
    Foundation = 0,
    Announcement = 1,
    ConstructionStart = 2,
    ConstructionMilestone = 3,
    Opening = 4,
    SeasonOpening = 5,
    Expansion = 6,
    AreaOpening = 7,
    AttractionOpening = 8,
    AttractionClosure = 9,
    Closure = 10,
    Reopening = 11,
    TemporaryClosure = 12,
    DefinitiveClosure = 13,
    Rename = 14,
    BrandingChange = 15,
    LogoChange = 16,
    OwnershipChange = 17,
    OperatorChange = 18,
    FounderMilestone = 19,
    Acquisition = 20,
    Sale = 21,
    Bankruptcy = 22,
    Liquidation = 23,
    LegalDispute = 24,
    Investment = 25,
    Masterplan = 26,
    InfrastructureChange = 27,
    TransportChange = 28,
    HotelOpening = 29,
    ResortExpansion = 30,
    ThemedAreaChange = 31,
    ParadeOrShowLaunch = 32,
    FestivalLaunch = 33,
    RecordOrAward = 34,
    AttendanceMilestone = 35,
    SafetyIncident = 36,
    Accident = 37,
    OperationalIncident = 38,
    WeatherEvent = 39,
    Fire = 40,
    Flood = 41,
    StormDamage = 42,
    HealthCrisis = 43,
    SecurityEvent = 44,
    StrikeOrSocialMovement = 45,
    RegulatoryChange = 46,
    PreservationOrHeritage = 47,
    Demolition = 48,
    Redevelopment = 49,
    MaintenanceCampaign = 50,
    TechnologyChange = 51,
    SustainabilityChange = 52,
    GuestExperienceChange = 53,
    PricingOrTicketingChange = 54,
    Partnership = 55,
    MediaAppearance = 56,
    Other = 999,
}

public enum ParkItemHistoryEventType
{
    Announcement = 0,
    DesignStart = 1,
    ConstructionStart = 2,
    ConstructionMilestone = 3,
    TestingStart = 4,
    SoftOpening = 5,
    Opening = 6,
    SeasonOpening = 7,
    Closure = 8,
    TemporaryClosure = 9,
    DefinitiveClosure = 10,
    Reopening = 11,
    Refurbishment = 12,
    Rehab = 13,
    Retrack = 14,
    LayoutChange = 15,
    RideSystemChange = 16,
    CapacityChange = 17,
    TrainChange = 18,
    VehicleChange = 19,
    RestraintChange = 20,
    ManufacturerChange = 21,
    ModelChange = 22,
    Rename = 23,
    ThemeChange = 24,
    StoryChange = 25,
    LogoChange = 26,
    SponsorChange = 27,
    AccessibilityChange = 28,
    HeightRequirementChange = 29,
    QueueChange = 30,
    FastPassChange = 31,
    RelocationDeparture = 32,
    RelocationArrival = 33,
    Dismantling = 34,
    Storage = 35,
    Sale = 36,
    Acquisition = 37,
    Transfer = 38,
    Reinstallation = 39,
    Accident = 40,
    Incident = 41,
    SafetyModification = 42,
    Fire = 43,
    WeatherDamage = 44,
    TechnicalFailure = 45,
    OperationalChange = 46,
    RecordOrAward = 47,
    MediaAppearance = 48,
    PreservationOrHeritage = 49,
    Demolition = 50,
    Replacement = 51,
    Other = 999,
}

public enum HistoryArticleBlockType
{
    Heading = 0,
    Paragraph = 1,
    Quote = 2,
    Image = 3,
    Gallery = 4,
    FactBox = 5,
    SourceNote = 6,
}

public sealed class HistorySourceReference
{
    public string? Label { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? AccessedAt { get; set; }
}

public sealed class HistoryArticleBlock
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public HistoryArticleBlockType Type { get; set; } = HistoryArticleBlockType.Paragraph;

    public int SortOrder { get; set; }

    public int? HeadingLevel { get; set; }

    public List<LocalizedText> Texts { get; set; } = new();

    public string? ImageId { get; set; }

    public List<string> ImageIds { get; set; } = new();

    public List<LocalizedText> Captions { get; set; } = new();
}

public sealed class HistoryArticle
{
    public string? Slug { get; set; }

    public List<LocalizedText> Titles { get; set; } = new();

    public List<LocalizedText> Subtitles { get; set; } = new();

    public List<LocalizedText> Summaries { get; set; } = new();

    public string? MainImageId { get; set; }

    public List<HistoryArticleBlock> Blocks { get; set; } = new();

    public List<HistorySourceReference> Sources { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}

public sealed class HistoryEvent : AuditableEntity
{
    public string Key { get; set; } = string.Empty;

    public HistoryEntityType EntityType { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public string? ParkId { get; set; }

    public string? ParkItemId { get; set; }

    public string? ContextParkId { get; set; }

    public int Year { get; set; }

    public int? Month { get; set; }

    public int? Day { get; set; }

    public HistoryDatePrecision DatePrecision { get; set; } = HistoryDatePrecision.Year;

    public string EventType { get; set; } = string.Empty;

    public bool IsMajor { get; set; }

    public bool IsVisible { get; set; } = true;

    public string? Slug { get; set; }

    public List<LocalizedText> Titles { get; set; } = new();

    public List<LocalizedText> Summaries { get; set; } = new();

    public string? MainImageId { get; set; }

    public string? PreviousName { get; set; }

    public string? NewName { get; set; }

    public string? PreviousLogoImageId { get; set; }

    public string? NewLogoImageId { get; set; }

    public string? PreviousOperatorId { get; set; }

    public string? NewOperatorId { get; set; }

    public string? LocationLabel { get; set; }

    public List<string> RelatedParkIds { get; set; } = new();

    public List<string> RelatedParkItemIds { get; set; } = new();

    public List<HistorySourceReference> Sources { get; set; } = new();

    public HistoryArticle? Article { get; set; }
}
