using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Référence minimale permettant de vérifier une décision.
/// </summary>
public sealed class AttractionCompatibilitySourceReference
{
    public AttractionCompatibilitySourceReference(AttractionAccessCondition condition)
        : this(new[] { condition ?? throw new ArgumentNullException(nameof(condition)) })
    {
    }

    internal AttractionCompatibilitySourceReference(
        IReadOnlyCollection<AttractionAccessCondition> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        AttractionAccessCondition condition = conditions.FirstOrDefault()
            ?? throw new ArgumentException(
                "At least one condition is required.",
                nameof(conditions));

        this.Kind = condition.SourceKind;
        this.Url = Normalize(condition.SourceUrl);
        this.Reference = Normalize(condition.SourceReference);
        this.LanguageCode = Normalize(condition.SourceLanguageCode);
        this.CollectedAtUtc = condition.CollectedAtUtc;
        this.VerifiedAtUtc = condition.VerifiedAtUtc;
        this.Confidence = condition.SourceConfidence;
        this.Summaries = conditions
            .SelectMany(static item => item.SourceSummary)
            .Select(static summary => new LocalizedText(summary.LanguageCode, summary.Value))
            .DistinctBy(static summary => new { summary.LanguageCode, summary.Value })
            .OrderBy(static summary => summary.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static summary => summary.LanguageCode, StringComparer.Ordinal)
            .ThenBy(static summary => summary.Value, StringComparer.Ordinal)
            .ToList();
    }

    public AttractionAccessConditionSourceKind Kind { get; }

    public string? Url { get; }

    public string? Reference { get; }

    public string? LanguageCode { get; }

    public DateTime? CollectedAtUtc { get; }

    public DateTime? VerifiedAtUtc { get; }

    public AttractionAccessConditionConfidence Confidence { get; }

    public IReadOnlyCollection<LocalizedText> Summaries { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
