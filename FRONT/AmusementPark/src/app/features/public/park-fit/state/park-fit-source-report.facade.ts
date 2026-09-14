import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';

import { ParkFitSourceReportRequest } from '@app/models/park-fit/park-fit-search.models';
import {
  PARK_FIT_SOURCE_REPORT_DATA_PORT,
  ParkFitSourceReportDataPort
} from './park-fit-source-report-data.port';

export type ParkFitSourceReportSubmissionStatus =
  'idle' | 'submitting' | 'success' | 'error' | 'rateLimited';

@Injectable()
export class ParkFitSourceReportFacade {
  private readonly statusSignal = signal<ParkFitSourceReportSubmissionStatus>('idle');

  public readonly status: Signal<ParkFitSourceReportSubmissionStatus> =
    this.statusSignal.asReadonly();

  constructor(
    @Inject(PARK_FIT_SOURCE_REPORT_DATA_PORT)
    private readonly dataPort: ParkFitSourceReportDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  submit(request: ParkFitSourceReportRequest): void {
    if (this.statusSignal() === 'submitting' || this.statusSignal() === 'success') {
      return;
    }

    this.statusSignal.set('submitting');
    this.dataPort.submitReport(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => this.statusSignal.set('success'),
        error: (error: unknown): void => this.statusSignal.set(
          error instanceof HttpErrorResponse && error.status === 429
            ? 'rateLimited'
            : 'error')
      });
  }

  reset(): void {
    if (this.statusSignal() !== 'submitting') {
      this.statusSignal.set('idle');
    }
  }
}
