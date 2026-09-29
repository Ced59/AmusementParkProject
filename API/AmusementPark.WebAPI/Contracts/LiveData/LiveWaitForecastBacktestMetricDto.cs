namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveWaitForecastBacktestMetricDto
{
    public string Method { get; set; } = string.Empty;

    public double MeanAbsoluteErrorMinutes { get; set; }

    public double MedianAbsoluteErrorMinutes { get; set; }

    public double P90AbsoluteErrorMinutes { get; set; }
}
