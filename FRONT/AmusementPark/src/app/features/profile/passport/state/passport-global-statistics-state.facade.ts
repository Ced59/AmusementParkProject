import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PassportGlobalStatistics } from '@app/models/passport/passport-statistics.models';
import {
  PASSPORT_GLOBAL_STATISTICS_FILTER_STORE,
  PassportGlobalStatisticsFilter,
  PassportGlobalStatisticsFilterStorePort
} from './passport-global-statistics-filter.ports';
import {
  PASSPORT_STATISTICS_API_PORT,
  PassportStatisticsApiPort
} from './passport-statistics-state-data.ports';

@Injectable()
export class PassportGlobalStatisticsStateFacade {
  private readonly statisticsSignal = signal<PassportGlobalStatistics | null>(null);
  private readonly filterSignal = signal<PassportGlobalStatisticsFilter>({ year: null, parkId: null });
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private lastSuccessfulFilter: PassportGlobalStatisticsFilter = { year: null, parkId: null };
  private loadGeneration: number = 0;

  readonly statistics: Signal<PassportGlobalStatistics | null> = this.statisticsSignal.asReadonly();
  readonly filter: Signal<PassportGlobalStatisticsFilter> = this.filterSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(PASSPORT_STATISTICS_API_PORT) private readonly statisticsApi: PassportStatisticsApiPort,
    @Inject(PASSPORT_GLOBAL_STATISTICS_FILTER_STORE)
    private readonly filterStore: PassportGlobalStatisticsFilterStorePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    this.filterSignal.set(this.filterStore.read());
    this.loadCurrentFilter();
  }

  selectYear(year: number | null): void {
    this.updateFilter({ ...this.filterSignal(), year });
  }

  selectPark(parkId: string | null): void {
    this.updateFilter({ ...this.filterSignal(), parkId: parkId?.trim() || null });
  }

  clearFilters(): void {
    this.updateFilter({ year: null, parkId: null });
  }

  retry(): void {
    this.loadCurrentFilter();
  }

  private updateFilter(filter: PassportGlobalStatisticsFilter): void {
    this.filterSignal.set(filter);
    this.filterStore.write(filter);
    this.loadCurrentFilter();
  }

  private loadCurrentFilter(): void {
    const generation: number = ++this.loadGeneration;
    const filter: PassportGlobalStatisticsFilter = this.filterSignal();
    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);
    this.statisticsApi.getGlobalStatistics(filter.year, filter.parkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (statistics: PassportGlobalStatistics): void => {
          if (generation !== this.loadGeneration) {
            return;
          }

          const availableFilter: PassportGlobalStatisticsFilter = this.keepAvailableFilter(filter, statistics);
          if (availableFilter.year !== filter.year || availableFilter.parkId !== filter.parkId) {
            this.updateFilter(availableFilter);
            return;
          }

          this.statisticsSignal.set(statistics);
          this.lastSuccessfulFilter = { ...filter };
          this.loadingSignal.set(false);
        },
        error: (error: unknown): void => {
          if (generation !== this.loadGeneration) {
            return;
          }
          this.loadingSignal.set(false);
          this.filterSignal.set({ ...this.lastSuccessfulFilter });
          this.filterStore.write(this.lastSuccessfulFilter);
          this.errorKeySignal.set(error instanceof HttpErrorResponse && error.status === 400
            ? 'passport.globalStatistics.errors.invalidFilter'
            : 'passport.globalStatistics.errors.load');
        }
      });
  }

  private keepAvailableFilter(
    filter: PassportGlobalStatisticsFilter,
    statistics: PassportGlobalStatistics
  ): PassportGlobalStatisticsFilter {
    const year: number | null = filter.year !== null && statistics.availableYears.includes(filter.year)
      ? filter.year
      : null;
    const parkId: string | null = filter.parkId !== null && statistics.availableParks.some(
      (park): boolean => park.parkId === filter.parkId
    )
      ? filter.parkId
      : null;
    return { year, parkId };
  }
}
