import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, map, Observable, of } from 'rxjs';

import {
  PassportItemStatistics,
  PassportParkStatistics,
  PassportYearStatistics
} from '@app/models/passport/passport-statistics.models';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import {
  mapItemStatisticsView,
  mapParkStatisticsView,
  mapYearStatisticsView
} from '../mappers/passport-statistics-view.mapper';
import {
  PassportStatisticsRouteScope,
  PassportStatisticsViewModel
} from '../models/passport-statistics-view.models';
import {
  PASSPORT_STATISTICS_API_PORT,
  PASSPORT_STATISTICS_ITEMS_PORT,
  PASSPORT_STATISTICS_PARKS_PORT,
  PassportStatisticsApiPort,
  PassportStatisticsItemsPort,
  PassportStatisticsParksPort
} from './passport-statistics-state-data.ports';

type PassportStatisticsSource =
  | { kind: 'item'; statistics: PassportItemStatistics; targetName: string | null }
  | { kind: 'park'; statistics: PassportParkStatistics; targetName: string | null }
  | { kind: 'year'; statistics: PassportYearStatistics };

@Injectable()
export class PassportStatisticsStateFacade {
  private readonly viewModelSignal = signal<PassportStatisticsViewModel | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private currentScope: PassportStatisticsRouteScope | null = null;
  private currentLanguage: string = 'en';
  private currentSource: PassportStatisticsSource | null = null;
  private loadGeneration: number = 0;

  readonly viewModel: Signal<PassportStatisticsViewModel | null> = this.viewModelSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(PASSPORT_STATISTICS_API_PORT) private readonly statisticsApi: PassportStatisticsApiPort,
    @Inject(PASSPORT_STATISTICS_PARKS_PORT) private readonly parksApi: PassportStatisticsParksPort,
    @Inject(PASSPORT_STATISTICS_ITEMS_PORT) private readonly itemsApi: PassportStatisticsItemsPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(scope: PassportStatisticsRouteScope, language: string): void {
    const generation: number = ++this.loadGeneration;
    this.currentScope = scope;
    this.currentLanguage = language;
    this.currentSource = null;
    this.viewModelSignal.set(null);
    this.errorKeySignal.set(null);

    if (!scope.targetId.trim() || (scope.kind === 'year' && !this.isValidYear(scope.targetId))) {
      this.loadingSignal.set(false);
      this.errorKeySignal.set('passport.statistics.errors.invalidScope');
      return;
    }

    this.loadingSignal.set(true);
    this.loadSource(scope).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (source: PassportStatisticsSource): void => {
        if (generation !== this.loadGeneration) {
          return;
        }

        this.currentSource = source;
        this.viewModelSignal.set(this.mapSource(source, this.currentLanguage));
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        if (generation !== this.loadGeneration) {
          return;
        }

        this.loadingSignal.set(false);
        this.errorKeySignal.set(this.resolveErrorKey(error));
      }
    });
  }

  retry(): void {
    if (this.currentScope) {
      this.load(this.currentScope, this.currentLanguage);
    }
  }

  changeLanguage(language: string): void {
    this.currentLanguage = language;
    if (this.currentSource) {
      this.viewModelSignal.set(this.mapSource(this.currentSource, language));
    }
  }

  private loadSource(scope: PassportStatisticsRouteScope): Observable<PassportStatisticsSource> {
    if (scope.kind === 'item') {
      return forkJoin({
        statistics: this.statisticsApi.getItemStatistics(scope.targetId),
        target: this.itemsApi.getParkItemById(scope.targetId, { closedFilter: 'all' }).pipe(
          catchError(() => of<ParkItem | null>(null))
        )
      }).pipe(map(({ statistics, target }): PassportStatisticsSource => ({
        kind: 'item',
        statistics,
        targetName: target?.name ?? null
      })));
    }

    if (scope.kind === 'park') {
      return forkJoin({
        statistics: this.statisticsApi.getParkStatistics(scope.targetId),
        target: this.parksApi.getParkById(scope.targetId, { closedFilter: 'all' }).pipe(
          catchError(() => of<Park | null>(null))
        )
      }).pipe(map(({ statistics, target }): PassportStatisticsSource => ({
        kind: 'park',
        statistics,
        targetName: target?.name ?? null
      })));
    }

    return this.statisticsApi.getYearStatistics(Number(scope.targetId)).pipe(
      map((statistics: PassportYearStatistics): PassportStatisticsSource => ({ kind: 'year', statistics }))
    );
  }

  private mapSource(source: PassportStatisticsSource, language: string): PassportStatisticsViewModel {
    if (source.kind === 'item') {
      return mapItemStatisticsView(source.statistics, source.targetName, language);
    }

    if (source.kind === 'park') {
      return mapParkStatisticsView(source.statistics, source.targetName, language);
    }

    return mapYearStatisticsView(source.statistics, language);
  }

  private isValidYear(value: string): boolean {
    const year: number = Number(value);
    return Number.isInteger(year) && year >= 1 && year <= 9999;
  }

  private resolveErrorKey(error: unknown): string {
    return error instanceof HttpErrorResponse && error.status === 404
      ? 'passport.statistics.errors.notFound'
      : 'passport.statistics.errors.load';
  }
}
