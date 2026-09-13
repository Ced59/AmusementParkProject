using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Sharing;

public readonly record struct ShareModerationReportId
{
    private readonly string? value;

    private ShareModerationReportId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized moderation report identifier has no value.");

    public static ShareModerationReportId New()
    {
        return new ShareModerationReportId(Guid.NewGuid().ToString("N"));
    }

    public static ShareModerationReportId Parse(string? value)
    {
        return new ShareModerationReportId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out ShareModerationReportId reportId)
    {
        try
        {
            reportId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            reportId = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            reportId = default;
            return false;
        }
    }
}
