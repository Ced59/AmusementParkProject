export type ParkWeatherRunStatus = 'Queued' | 'Running' | 'Completed' | 'CompletedWithFailures' | 'Failed' | 'Skipped';

export interface ParkWeatherRun {
  id: string;
  trigger: string;
  scope: string;
  status: ParkWeatherRunStatus | string;
  sourceRunId?: string | null;
  targetParkId?: string | null;
  cancelsAutomaticRunLocalDate?: string | null;
  requestedAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  totalParkCount: number;
  succeededParkCount: number;
  failedParkCount: number;
  skippedParkCount: number;
  warningParkCount: number;
  message?: string | null;
}

export interface ParkWeatherRunItem {
  id: string;
  runId: string;
  parkId: string;
  parkName?: string | null;
  status: string;
  attemptCount: number;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  forecastDayCount: number;
  observationDayCount: number;
  warningMessage?: string | null;
  errorCode?: string | null;
  errorMessage?: string | null;
}
