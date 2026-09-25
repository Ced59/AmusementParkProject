using System.Globalization;

namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Sélection civile demandée pour un snapshot historique.
/// Une année ou un mois représente toute son enveloppe.
/// </summary>
public sealed record HistoricalInstant
{
    public HistoricalInstant(
        int year,
        int? month,
        int? day,
        HistoryDatePrecision precision)
    {
        HistoricalDate validatedDate = new HistoricalDate(
            year,
            month,
            day,
            precision,
            false,
            null);

        this.Year = validatedDate.Year;
        this.Month = validatedDate.Month;
        this.Day = validatedDate.Day;
        this.Precision = validatedDate.Precision;
    }

    public int Year { get; }

    public int? Month { get; }

    public int? Day { get; }

    public HistoryDatePrecision Precision { get; }

    public string CacheKey => this.Precision switch
    {
        HistoryDatePrecision.Year => string.Create(
            CultureInfo.InvariantCulture,
            $"Y:{this.Year:D4}"),
        HistoryDatePrecision.Month => string.Create(
            CultureInfo.InvariantCulture,
            $"M:{this.Year:D4}-{this.Month!.Value:D2}"),
        HistoryDatePrecision.Day => string.Create(
            CultureInfo.InvariantCulture,
            $"D:{this.Year:D4}-{this.Month!.Value:D2}-{this.Day!.Value:D2}"),
        _ => throw new InvalidOperationException("The historical instant precision is invalid."),
    };

    public static HistoricalInstant ForYear(int year)
    {
        return new HistoricalInstant(year, null, null, HistoryDatePrecision.Year);
    }

    public static HistoricalInstant ForMonth(int year, int month)
    {
        return new HistoricalInstant(year, month, null, HistoryDatePrecision.Month);
    }

    public static HistoricalInstant ForDay(int year, int month, int day)
    {
        return new HistoricalInstant(year, month, day, HistoryDatePrecision.Day);
    }

    public HistoricalDateEnvelope GetEnvelope()
    {
        HistoricalDate date = new HistoricalDate(
            this.Year,
            this.Month,
            this.Day,
            this.Precision,
            false,
            null);
        HistoricalDateEnvelope envelope = date.GetEnvelope();

        return new HistoricalDateEnvelope(
            envelope.EarliestPossibleDate,
            envelope.LatestPossibleDate,
            false);
    }

    public override string ToString()
    {
        return this.CacheKey[2..];
    }
}
