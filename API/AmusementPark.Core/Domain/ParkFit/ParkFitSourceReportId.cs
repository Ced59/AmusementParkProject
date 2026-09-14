using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.ParkFit;

public readonly record struct ParkFitSourceReportId
{
    private readonly string? value;

    private ParkFitSourceReportId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized Park Fit report identifier has no value.");

    public static ParkFitSourceReportId New()
    {
        return new ParkFitSourceReportId(Guid.NewGuid().ToString("N"));
    }

    public static ParkFitSourceReportId Parse(string? value)
    {
        return new ParkFitSourceReportId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out ParkFitSourceReportId reportId)
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
