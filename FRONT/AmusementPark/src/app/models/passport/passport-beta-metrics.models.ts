export type PassportBetaRepeatUsageSignal = 'NotObserved' | 'Emerging' | 'Candidate';

export interface PassportBetaMetricsQuery {
  readonly fromUtc?: string | null;
  readonly toUtc?: string | null;
}

export interface PassportBetaDailyMetrics {
  readonly date: string;
  readonly completedVisits: number;
  readonly firstVisits: number;
  readonly secondVisits: number;
}

export interface PassportBetaMetricsResult {
  readonly generatedAtUtc: string;
  readonly fromUtc: string;
  readonly toUtc: string;
  readonly createdVisits: number;
  readonly completedVisits: number;
  readonly usersWithCompletedVisit: number;
  readonly usersWithSecondCompletedVisit: number;
  readonly repeatUsageRatePercent: number;
  readonly repeatUsageSignal: PassportBetaRepeatUsageSignal;
  readonly requiresQualitativeValidation: boolean;
  readonly daily: readonly PassportBetaDailyMetrics[];
}
