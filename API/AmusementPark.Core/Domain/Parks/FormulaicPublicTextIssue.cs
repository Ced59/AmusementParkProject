namespace AmusementPark.Core.Domain.Parks;

public sealed record FormulaicPublicTextIssue
{
    public required string MatchType { get; init; }

    public required string LanguageCode { get; init; }

    public required int FirstDocumentIndex { get; init; }

    public required int SecondDocumentIndex { get; init; }

    public required string FirstDocumentSha256 { get; init; }

    public required string SecondDocumentSha256 { get; init; }

    public required string FingerprintSha256 { get; init; }
}
