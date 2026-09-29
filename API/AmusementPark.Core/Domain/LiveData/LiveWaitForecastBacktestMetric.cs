namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitForecastBacktestMetric
{
    public LiveWaitForecastBacktestMetric(
        string method,
        double meanAbsoluteErrorMinutes,
        double medianAbsoluteErrorMinutes,
        double p90AbsoluteErrorMinutes)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("A backtest method is required.", nameof(method));
        }

        this.Method = method.Trim();
        this.MeanAbsoluteErrorMinutes = meanAbsoluteErrorMinutes;
        this.MedianAbsoluteErrorMinutes = medianAbsoluteErrorMinutes;
        this.P90AbsoluteErrorMinutes = p90AbsoluteErrorMinutes;
    }

    public string Method { get; }

    public double MeanAbsoluteErrorMinutes { get; }

    public double MedianAbsoluteErrorMinutes { get; }

    public double P90AbsoluteErrorMinutes { get; }
}
