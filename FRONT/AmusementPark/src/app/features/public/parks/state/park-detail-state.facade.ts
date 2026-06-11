import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkDetailSummary, ParkDetailSummaryStats } from '@app/models/parks/park-detail-summary';
import { ParkExplorer, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { mapNullable } from '@shared/utils/mapping';
import { mapParkContentSummaryViewModel } from '../mappers/park-content-summary.mapper';
import { mapParkToDetailViewModel } from '../mappers/park-detail-view.mapper';
import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import {
  PARK_DETAIL_PARKS_PORT,
  ParkDetailParksPort
} from './park-detail-data.ports';

@Injectable()
export class ParkDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkDetailSummary>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly park: Signal<ParkDetailViewModel | null> = computed(() => {
    const summary: ParkDetailSummary | undefined = this.screenStateStore.data();

    return mapNullable(summary, (currentSummary: ParkDetailSummary) => {
      return mapParkToDetailViewModel(
        currentSummary.park,
        this.currentLanguageSignal(),
        {
          founderName: currentSummary.references?.founderName ?? null,
          operatorName: currentSummary.references?.operatorName ?? null,
          countryName: this.countryDisplayService.resolveLocalizedCountryName(currentSummary.park.countryCode, this.currentLanguageSignal())
        },
        {
          totalItems: currentSummary.stats?.totalItems ?? null,
          zoneCount: currentSummary.stats?.zoneCount ?? null
        },
        currentSummary.mainImage ? [currentSummary.mainImage] : []
      );
    });
  });
  public readonly summary: Signal<ParkContentSummaryViewModel | null> = computed(() => {
    return mapParkContentSummaryViewModel(this.park(), this.toExplorerSummary(this.screenStateStore.data()));
  });

  constructor(
    @Inject(PARK_DETAIL_PARKS_PORT) private readonly parksApiService: ParkDetailParksPort,
    private readonly countryDisplayService: CountryDisplayService,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadPark(id: string): void {
    const previousData: ParkDetailSummary | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parksApiService.getParkDetailSummary(id, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (summary: ParkDetailSummary) => {
        this.screenStateStore.setReady(summary);
      },
      error: (error: unknown) => {
        console.error('Error loading park detail summary', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parks.detail.errorMessage', previousData);
      }
    });
  }

  private toExplorerSummary(summary: ParkDetailSummary | undefined): ParkExplorer | null {
    if (!summary?.park.id || !summary.stats) {
      return null;
    }

    const categoryCounts: ParkExplorerCount[] = this.toCategoryCounts(summary.stats);

    return {
      parkId: summary.park.id,
      hasZones: summary.stats.zoneCount > 0,
      overview: {
        id: null,
        name: 'overview',
        names: [],
        slug: null,
        isVirtual: true,
        totalItems: summary.stats.totalItems,
        countsByCategory: categoryCounts,
        countsByType: []
      },
      zones: [],
      unassigned: null
    };
  }

  private toCategoryCounts(stats: ParkDetailSummaryStats): ParkExplorerCount[] {
    return Object.entries(stats.countsByCategory ?? {})
      .map(([key, count]: [string, number]) => ({ key, count }))
      .filter((entry: ParkExplorerCount) => entry.count > 0);
  }
}
