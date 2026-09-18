using System.Globalization;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripInvitationPeriodPreview
{
    private const string MonthFormat = "yyyy-MM";

    private TripInvitationPeriodPreview(
        TripInvitationPeriodKind kind,
        string? startMonth,
        string? endMonth)
    {
        if (!Enum.IsDefined(kind))
        {
            throw Invalid();
        }

        string? normalizedStart = NormalizeMonth(startMonth);
        string? normalizedEnd = NormalizeMonth(endMonth);
        bool valid = kind switch
        {
            TripInvitationPeriodKind.Unspecified => normalizedStart is null && normalizedEnd is null,
            TripInvitationPeriodKind.SingleMonth => normalizedStart is not null
                && string.Equals(normalizedStart, normalizedEnd, StringComparison.Ordinal),
            TripInvitationPeriodKind.MonthRange => normalizedStart is not null
                && normalizedEnd is not null
                && string.CompareOrdinal(normalizedStart, normalizedEnd) < 0,
            _ => false,
        };
        if (!valid)
        {
            throw Invalid();
        }

        this.Kind = kind;
        this.StartMonth = normalizedStart;
        this.EndMonth = normalizedEnd;
    }

    public TripInvitationPeriodKind Kind { get; }

    public string? StartMonth { get; }

    public string? EndMonth { get; }

    public static TripInvitationPeriodPreview Unspecified()
    {
        return new TripInvitationPeriodPreview(TripInvitationPeriodKind.Unspecified, null, null);
    }

    public static TripInvitationPeriodPreview FromDates(IEnumerable<DateOnly> dates)
    {
        ArgumentNullException.ThrowIfNull(dates);
        DateOnly[] normalizedDates = dates.Distinct().OrderBy(static date => date).ToArray();
        if (normalizedDates.Length == 0)
        {
            return Unspecified();
        }

        string startMonth = normalizedDates[0].ToString(MonthFormat, CultureInfo.InvariantCulture);
        string endMonth = normalizedDates[^1].ToString(MonthFormat, CultureInfo.InvariantCulture);
        return new TripInvitationPeriodPreview(
            string.Equals(startMonth, endMonth, StringComparison.Ordinal)
                ? TripInvitationPeriodKind.SingleMonth
                : TripInvitationPeriodKind.MonthRange,
            startMonth,
            endMonth);
    }

    public static TripInvitationPeriodPreview Restore(
        TripInvitationPeriodKind kind,
        string? startMonth,
        string? endMonth)
    {
        return new TripInvitationPeriodPreview(kind, startMonth, endMonth);
    }

    private static string? NormalizeMonth(string? value)
    {
        string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is null)
        {
            return null;
        }

        return DateOnly.TryParseExact(
            $"{normalized}-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _)
            ? normalized
            : throw Invalid();
    }

    private static TripInvitationValidationException Invalid()
    {
        return new TripInvitationValidationException(
            TripInvitationErrorCodes.InvalidPreview,
            "The trip invitation period preview is invalid.");
    }
}
