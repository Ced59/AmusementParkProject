import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PassportHistoricalStatistics } from '@app/models/passport/passport-statistics.models';
import {
  PASSPORT_STATISTICS_API_PORT,
  PassportStatisticsApiPort
} from './passport-statistics-state-data.ports';

@Injectable()
export class PassportHistoricalStatisticsStateFacade {
  private readonly statisticsSignal = signal<PassportHistoricalStatistics | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);

  readonly statistics: Signal<PassportHistoricalStatistics | null> = this.statisticsSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(PASSPORT_STATISTICS_API_PORT) private readonly statisticsApi: PassportStatisticsApiPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    if (this.loadingSignal()) {
      return;
    }

    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);
    this.statisticsApi.getHistoricalStatistics()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (statistics: PassportHistoricalStatistics): void => {
          this.statisticsSignal.set(statistics);
          this.loadingSignal.set(false);
        },
        error: (error: unknown): void => {
          this.loadingSignal.set(false);
          this.errorKeySignal.set(error instanceof HttpErrorResponse && error.status === 401
            ? 'passport.historicalStatistics.errors.authentication'
            : 'passport.historicalStatistics.errors.load');
        }
      });
  }
}
