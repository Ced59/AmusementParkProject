import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  PassportHistoricalExistenceReport,
  SubmitPassportHistoricalExistenceReportRequest
} from '@app/models/passport/passport-historical-existence-report.models';
import {
  PASSPORT_HISTORICAL_EXISTENCE_REPORT_DATA_PORT,
  PassportHistoricalExistenceReportDataPort
} from './passport-historical-existence-report-data.port';

export type PassportHistoricalExistenceReportSubmissionStatus =
  'idle' | 'submitting' | 'success' | 'conflict' | 'rateLimited' | 'error';

@Injectable()
export class PassportHistoricalExistenceReportFacade {
  private readonly reportsSignal = signal<readonly PassportHistoricalExistenceReport[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadErrorSignal = signal<boolean>(false);
  private readonly submissionStatusSignal =
    signal<PassportHistoricalExistenceReportSubmissionStatus>('idle');

  public readonly reports: Signal<readonly PassportHistoricalExistenceReport[]> =
    this.reportsSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly loadError: Signal<boolean> = this.loadErrorSignal.asReadonly();
  public readonly submissionStatus: Signal<PassportHistoricalExistenceReportSubmissionStatus> =
    this.submissionStatusSignal.asReadonly();

  constructor(
    @Inject(PASSPORT_HISTORICAL_EXISTENCE_REPORT_DATA_PORT)
    private readonly dataPort: PassportHistoricalExistenceReportDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(visitId: string): void {
    const normalizedVisitId: string = visitId.trim();
    if (!normalizedVisitId) {
      return;
    }

    this.loadingSignal.set(true);
    this.loadErrorSignal.set(false);
    this.dataPort.list(normalizedVisitId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (reports: PassportHistoricalExistenceReport[]): void => {
          this.reportsSignal.set(reports);
          this.loadingSignal.set(false);
        },
        error: (): void => {
          this.loadingSignal.set(false);
          this.loadErrorSignal.set(true);
        }
      });
  }

  submit(
    visitId: string,
    request: SubmitPassportHistoricalExistenceReportRequest
  ): void {
    if (this.submissionStatusSignal() === 'submitting') {
      return;
    }

    this.submissionStatusSignal.set('submitting');
    this.dataPort.submit(visitId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report: PassportHistoricalExistenceReport): void => {
          this.reportsSignal.update(
            (reports: readonly PassportHistoricalExistenceReport[]) => [report, ...reports]);
          this.submissionStatusSignal.set('success');
        },
        error: (error: unknown): void => {
          if (error instanceof HttpErrorResponse && error.status === 409) {
            this.submissionStatusSignal.set('conflict');
          } else if (error instanceof HttpErrorResponse && error.status === 429) {
            this.submissionStatusSignal.set('rateLimited');
          } else {
            this.submissionStatusSignal.set('error');
          }
        }
      });
  }

  resetSubmission(): void {
    if (this.submissionStatusSignal() !== 'submitting') {
      this.submissionStatusSignal.set('idle');
    }
  }
}
