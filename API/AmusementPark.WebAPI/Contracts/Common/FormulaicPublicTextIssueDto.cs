namespace AmusementPark.WebAPI.Contracts.Common;

public sealed class FormulaicPublicTextIssueDto
{
    public string MatchType { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public int FirstDocumentIndex { get; set; }

    public int SecondDocumentIndex { get; set; }
}
