namespace AmusementPark.Application.Features.ContextualBlocks;

internal static class ContextualBlockContracts
{
    public const string DocumentType = "AmusementParkContextualBlockUpsert";

    public const string ParkDescriptionBlockType = "park.description";

    public const string ParkPracticalBlockType = "park.practical";

    public static readonly string[] SupportedLanguageCodes = new string[]
    {
        "en",
        "fr",
        "es",
        "de",
        "it",
        "pl",
        "nl",
        "pt",
    };

    public static bool IsSupportedBlockType(string blockType)
    {
        return string.Equals(blockType, ParkDescriptionBlockType, StringComparison.Ordinal)
            || string.Equals(blockType, ParkPracticalBlockType, StringComparison.Ordinal);
    }
}
