using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.History;

public readonly record struct HistoricalExistenceReportId
{
    private readonly string? value;

    private HistoricalExistenceReportId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized report identifier has no value.");

    public static HistoricalExistenceReportId New()
    {
        return new HistoricalExistenceReportId(Guid.NewGuid().ToString("N"));
    }

    public static HistoricalExistenceReportId Parse(string? value)
    {
        return new HistoricalExistenceReportId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out HistoricalExistenceReportId reportId)
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
    }

    public override string ToString()
    {
        return this.Value;
    }
}
