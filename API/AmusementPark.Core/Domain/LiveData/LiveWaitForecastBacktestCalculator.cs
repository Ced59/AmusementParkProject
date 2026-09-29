namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitForecastBacktestCalculator
{
    private readonly LiveWaitForecastBacktestPolicy policy;

    public LiveWaitForecastBacktestCalculator(LiveWaitForecastBacktestPolicy policy)
    {
        this.policy = policy;
    }

    public LiveWaitForecastBacktestReport Calculate(
        IReadOnlyCollection<LiveWaitHistoryObservation> observations,
        DateTime evaluationFromUtc,
        DateTime evaluationToUtc,
        LivePollingActiveWindow activeWindow)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(activeWindow);
        EnsureUtc(evaluationFromUtc, nameof(evaluationFromUtc));
        EnsureUtc(evaluationToUtc, nameof(evaluationToUtc));
        if (evaluationFromUtc >= evaluationToUtc)
        {
            throw new ArgumentException("The backtest evaluation period must have a positive duration.");
        }

        LiveWaitHistoryObservation[] uniqueObservations = observations
            .Where(observation => observation.ObservedAtUtc < evaluationToUtc
                && observation.ReceivedAtUtc <= evaluationToUtc)
            .GroupBy(static observation => observation.ObservedAtUtc)
            .Select(static group => group
                .OrderByDescending(static observation => observation.ReceivedAtUtc)
                .First())
            .OrderBy(static observation => observation.ObservedAtUtc)
            .ToArray();
        (
            DateTime TimestampUtc,
            DateTime AvailableAtUtc,
            DateOnly LocalDate,
            DayOfWeek DayOfWeek,
            int LocalHour,
            double Wait)[] points = BuildHourlyPoints(uniqueObservations, activeWindow);
        Dictionary<int, List<(DateTime TimestampUtc, double Wait)>> baselineHistory =
            new Dictionary<int, List<(DateTime, double)>>();
        Dictionary<(DayOfWeek DayOfWeek, int LocalHour), List<(DateTime TimestampUtc, double Wait)>>
            candidateHistory =
                new Dictionary<(DayOfWeek, int), List<(DateTime, double)>>();
        PriorityQueue<int, DateTime> pendingTrainingPoints = new();
        List<(
            DateTime TimestampUtc,
            DateOnly LocalDate,
            double BaselineError,
            double CandidateError,
            bool IntervalHit,
            double IntervalWidth)> folds = new();

        for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
        {
            (
                DateTime TimestampUtc,
                DateTime AvailableAtUtc,
                DateOnly LocalDate,
                DayOfWeek DayOfWeek,
                int LocalHour,
                double Wait) point = points[pointIndex];
            while (pendingTrainingPoints.TryPeek(out int trainingPointIndex, out DateTime availableAtUtc)
                && availableAtUtc <= point.TimestampUtc)
            {
                pendingTrainingPoints.Dequeue();
                (
                    DateTime TimestampUtc,
                    DateTime AvailableAtUtc,
                    DateOnly LocalDate,
                    DayOfWeek DayOfWeek,
                    int LocalHour,
                    double Wait) trainingPoint = points[trainingPointIndex];
                GetOrCreate(baselineHistory, trainingPoint.LocalHour)
                    .Add((trainingPoint.TimestampUtc, trainingPoint.Wait));
                GetOrCreate(
                        candidateHistory,
                        (trainingPoint.DayOfWeek, trainingPoint.LocalHour))
                    .Add((trainingPoint.TimestampUtc, trainingPoint.Wait));
            }

            List<(DateTime TimestampUtc, double Wait)> baselineValuesByTime = GetOrCreate(
                baselineHistory,
                point.LocalHour);
            List<(DateTime TimestampUtc, double Wait)> candidateValuesByTime = GetOrCreate(
                candidateHistory,
                (point.DayOfWeek, point.LocalHour));
            DateTime trainingStartsAtUtc = point.TimestampUtc.AddDays(-this.policy.TrainingWindowDays);
            Prune(baselineValuesByTime, trainingStartsAtUtc);
            Prune(candidateValuesByTime, trainingStartsAtUtc);

            if (point.TimestampUtc >= evaluationFromUtc
                && baselineValuesByTime.Count >= this.policy.MinimumBaselineTrainingDays
                && candidateValuesByTime.Count >= this.policy.MinimumCandidateTrainingDays)
            {
                double[] baselineValues = baselineValuesByTime
                    .Select(static value => value.Wait)
                    .Order()
                    .ToArray();
                double[] candidateValues = candidateValuesByTime
                    .Select(static value => value.Wait)
                    .Order()
                    .ToArray();
                double baselinePrediction = Percentile(baselineValues, 0.50d);
                double candidatePrediction = Percentile(candidateValues, 0.50d);
                double lowerBound = Percentile(candidateValues, 0.10d);
                double upperBound = Percentile(candidateValues, 0.90d);
                folds.Add((
                    point.TimestampUtc,
                    point.LocalDate,
                    Math.Abs(point.Wait - baselinePrediction),
                    Math.Abs(point.Wait - candidatePrediction),
                    point.Wait >= lowerBound && point.Wait <= upperBound,
                    upperBound - lowerBound));
            }

            pendingTrainingPoints.Enqueue(pointIndex, point.AvailableAtUtc);
        }

        return this.BuildReport(
            uniqueObservations.Length,
            points.Length,
            folds,
            evaluationFromUtc,
            evaluationToUtc,
            activeWindow.TimeZone.Id);
    }

    private LiveWaitForecastBacktestReport BuildReport(
        int sourceObservationCount,
        int hourlyPointCount,
        IReadOnlyCollection<(
            DateTime TimestampUtc,
            DateOnly LocalDate,
            double BaselineError,
            double CandidateError,
            bool IntervalHit,
            double IntervalWidth)> folds,
        DateTime evaluationFromUtc,
        DateTime evaluationToUtc,
        string timeZoneId)
    {
        int evaluationDays = folds.Select(static fold => fold.LocalDate).Distinct().Count();
        List<LiveWaitForecastBacktestReason> reasons = new();
        if (folds.Count < this.policy.MinimumEvaluationPoints)
        {
            reasons.Add(LiveWaitForecastBacktestReason.InsufficientEvaluationPoints);
        }

        if (evaluationDays < this.policy.MinimumEvaluationDays)
        {
            reasons.Add(LiveWaitForecastBacktestReason.InsufficientEvaluationDays);
        }

        if (reasons.Count > 0)
        {
            return new LiveWaitForecastBacktestReport(
                evaluationFromUtc,
                evaluationToUtc,
                timeZoneId,
                LiveWaitForecastBacktestVerdict.InsufficientData,
                reasons,
                sourceObservationCount,
                hourlyPointCount,
                folds.Count,
                evaluationDays,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                this.policy);
        }

        double[] baselineErrors = folds.Select(static fold => fold.BaselineError).Order().ToArray();
        double[] candidateErrors = folds.Select(static fold => fold.CandidateError).Order().ToArray();
        LiveWaitForecastBacktestMetric baseline = BuildMetric(
            LiveWaitForecastBacktestPolicy.BaselineMethod,
            baselineErrors);
        LiveWaitForecastBacktestMetric candidate = BuildMetric(
            LiveWaitForecastBacktestPolicy.CandidateMethod,
            candidateErrors);
        double baselineMeanAbsoluteError = MeanRaw(baselineErrors);
        double candidateMeanAbsoluteError = MeanRaw(candidateErrors);
        double rawImprovementPercent = this.policy.CalculateMaeImprovementPercent(
            baselineMeanAbsoluteError,
            candidateMeanAbsoluteError);
        double improvementPercent = Math.Round(rawImprovementPercent, 1);
        double rawIntervalCoveragePercent =
            folds.Count(static fold => fold.IntervalHit) * 100d / folds.Count;
        double intervalCoveragePercent = Math.Round(rawIntervalCoveragePercent, 1);
        double rawMedianIntervalWidth = Percentile(
            folds.Select(static fold => fold.IntervalWidth).Order().ToArray(),
            0.50d);
        double medianIntervalWidth = Math.Round(rawMedianIntervalWidth, 1);
        int half = folds.Count / 2;
        double rawOlderMae = MeanRaw(folds.Take(half).Select(static fold => fold.CandidateError));
        double rawRecentMae = MeanRaw(folds.Skip(half).Select(static fold => fold.CandidateError));
        double olderMae = Math.Round(rawOlderMae, 1);
        double recentMae = Math.Round(rawRecentMae, 1);
        double driftPercent = Math.Round(
            this.policy.CalculateDriftPercent(rawOlderMae, rawRecentMae),
            1);
        bool driftDetected = this.policy.IsDriftDetected(rawOlderMae, rawRecentMae);

        if (!this.policy.HasRequiredMaeImprovement(
            baselineMeanAbsoluteError,
            candidateMeanAbsoluteError))
        {
            reasons.Add(LiveWaitForecastBacktestReason.BaselineNotBeaten);
        }

        if (!this.policy.IsIntervalUseful(
            rawIntervalCoveragePercent,
            rawMedianIntervalWidth))
        {
            reasons.Add(LiveWaitForecastBacktestReason.IntervalMiscalibrated);
        }

        if (driftDetected)
        {
            reasons.Add(LiveWaitForecastBacktestReason.DriftDetected);
        }

        LiveWaitForecastBacktestVerdict verdict = reasons.Count == 0
            ? LiveWaitForecastBacktestVerdict.EligibleForPilot
            : LiveWaitForecastBacktestVerdict.Abandon;
        if (verdict == LiveWaitForecastBacktestVerdict.EligibleForPilot)
        {
            reasons.Add(LiveWaitForecastBacktestReason.CandidatePassed);
        }

        return new LiveWaitForecastBacktestReport(
            evaluationFromUtc,
            evaluationToUtc,
            timeZoneId,
            verdict,
            reasons,
            sourceObservationCount,
            hourlyPointCount,
            folds.Count,
            evaluationDays,
            baseline,
            candidate,
            improvementPercent,
            intervalCoveragePercent,
            medianIntervalWidth,
            olderMae,
            recentMae,
            driftPercent,
            driftDetected,
            this.policy);
    }

    private static (
        DateTime TimestampUtc,
        DateTime AvailableAtUtc,
        DateOnly LocalDate,
        DayOfWeek DayOfWeek,
        int LocalHour,
        double Wait)[]
        BuildHourlyPoints(
            IReadOnlyCollection<LiveWaitHistoryObservation> observations,
            LivePollingActiveWindow activeWindow)
    {
        return observations
            .Where(observation => !observation.IsBucketTruncated
                && activeWindow.Contains(observation.ObservedAtUtc)
                && IsOperating(observation.Status))
            .Select(observation => (
                Observation: observation,
                Wait: observation.Queues
                    .Where(static queue => queue.Kind == LiveQueueKind.Standby)
                    .Select(static queue => queue.WaitTimeMinutes)
                    .FirstOrDefault(static value => value.HasValue)))
            .Where(static value => value.Wait.HasValue)
            .Select(value => (
                value.Observation,
                Wait: value.Wait!.Value,
                Local: TimeZoneInfo.ConvertTimeFromUtc(
                    value.Observation.ObservedAtUtc,
                    activeWindow.TimeZone)))
            .GroupBy(static value => (DateOnly.FromDateTime(value.Local), value.Local.Hour))
            .Select(static group => (
                TimestampUtc: group.Min(static value => value.Observation.ObservedAtUtc),
                AvailableAtUtc: group.Max(static value => value.Observation.ReceivedAtUtc),
                LocalDate: group.Key.Item1,
                DayOfWeek: group.Key.Item1.DayOfWeek,
                LocalHour: group.Key.Hour,
                Wait: Percentile(group.Select(static value => (double)value.Wait).Order().ToArray(), 0.50d)))
            .OrderBy(static point => point.TimestampUtc)
            .ToArray();
    }

    private static List<(DateTime TimestampUtc, double Wait)> GetOrCreate<TKey>(
        IDictionary<TKey, List<(DateTime TimestampUtc, double Wait)>> histories,
        TKey key)
        where TKey : notnull
    {
        if (!histories.TryGetValue(key, out List<(DateTime TimestampUtc, double Wait)>? history))
        {
            history = new List<(DateTime, double)>();
            histories.Add(key, history);
        }

        return history;
    }

    private static void Prune(
        List<(DateTime TimestampUtc, double Wait)> history,
        DateTime startsAtUtc)
    {
        history.RemoveAll(value => value.TimestampUtc < startsAtUtc);
    }

    private static LiveWaitForecastBacktestMetric BuildMetric(
        string method,
        IReadOnlyList<double> sortedErrors)
    {
        return new LiveWaitForecastBacktestMetric(
            method,
            Math.Round(MeanRaw(sortedErrors), 1),
            Math.Round(Percentile(sortedErrors, 0.50d), 1),
            Math.Round(Percentile(sortedErrors, 0.90d), 1));
    }

    private static double MeanRaw(IEnumerable<double> values)
    {
        double[] materialized = values.ToArray();
        return materialized.Length == 0
            ? 0d
            : materialized.Average();
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0d;
        }

        double index = (sortedValues.Count - 1) * percentile;
        int lowerIndex = (int)Math.Floor(index);
        int upperIndex = (int)Math.Ceiling(index);
        return sortedValues[lowerIndex]
            + ((sortedValues[upperIndex] - sortedValues[lowerIndex]) * (index - lowerIndex));
    }

    private static bool IsOperating(LiveOperationalStatus status)
    {
        return status is LiveOperationalStatus.Open
            or LiveOperationalStatus.OperatingWithLimitations;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A backtest timestamp must be UTC.", parameterName);
        }
    }
}
