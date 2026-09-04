import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { map, Observable } from 'rxjs';

import {
  PassportItemStatistics,
  PassportParkStatistics,
  PassportYearStatistics
} from '@app/models/passport/passport-statistics.models';
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
  PassportStatisticsApiPort
} from './passport-statistics-state-data.ports';

type PassportStatisticsSource =
  | { kind: 'item'; statistics: PassportItemStatistics }
  | { kind: 'park'; statistics: PassportParkStatistics }
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
      return this.statisticsApi.getItemStatistics(scope.targetId).pipe(
        map((statistics: PassportItemStatistics): PassportStatisticsSource => ({
          kind: 'item',
          statistics
        })));
    }

    if (scope.kind === 'park') {
      return this.statisticsApi.getParkStatistics(scope.targetId).pipe(
        map((statistics: PassportParkStatistics): PassportStatisticsSource => ({
          kind: 'park',
          statistics
        })));
    }

    return this.statisticsApi.getYearStatistics(Number(scope.targetId)).pipe(
      map((statistics: PassportYearStatistics): PassportStatisticsSource => ({ kind: 'year', statistics }))
    );
  }

  private mapSource(source: PassportStatisticsSource, language: string): PassportStatisticsViewModel {
    if (source.kind === 'item') {
      return mapItemStatisticsView(source.statistics, language);
    }

    if (source.kind === 'park') {
      return mapParkStatisticsView(source.statistics, language);
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
