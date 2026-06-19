namespace AmusementPark.Application.Features.ParkWeather.Services;

public sealed class ParkWeatherHistoricalComparisonDateResolver
{
    public DateOnly ResolveComparisonDate(DateOnly forecastLocalDate, int yearsBack)
    {
        if (yearsBack <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(yearsBack), "Years back must be positive.");
        }

        int targetYear = forecastLocalDate.Year - yearsBack;
        if (forecastLocalDate.Month == 2 && forecastLocalDate.Day == 29 && !DateTime.IsLeapYear(targetYear))
        {
            return new DateOnly(targetYear, 2, 28);
        }

        return new DateOnly(targetYear, forecastLocalDate.Month, forecastLocalDate.Day);
    }

    public IReadOnlyCollection<DateOnly> ResolveComparisonDates(IReadOnlyCollection<DateOnly> forecastLocalDates, int historicalComparisonYears)
    {
        int years = Math.Clamp(historicalComparisonYears, 0, 3);
        if (forecastLocalDates.Count == 0 || years == 0)
        {
            return Array.Empty<DateOnly>();
        }

        HashSet<DateOnly> dates = new HashSet<DateOnly>();
        foreach (DateOnly forecastLocalDate in forecastLocalDates)
        {
            for (int yearsBack = 1; yearsBack <= years; yearsBack += 1)
            {
                dates.Add(this.ResolveComparisonDate(forecastLocalDate, yearsBack));
            }
        }

        return dates
            .OrderBy(static date => date)
            .ToList();
    }
}
