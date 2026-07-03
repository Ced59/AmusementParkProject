import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkMapItems } from '@app/models/parks/park-map-items';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { resolveParkSummarySocialImageId } from '@shared/utils/images/park-social-image.helpers';
import { mapParkMapItemsToViewModel } from '../mappers/park-map-items-view.mapper';
import { ParkItemsMapViewModel } from '../models/park-items-map-view.model';
import { PARK_MAP_PARKS_PORT, ParkMapParksPort } from './park-map-data.ports';
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';

interface ParkMapPageData {
  mapItems: ParkMapItems;
  parkImageId: string | null;
}

@Injectable()
export class ParkMapStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkMapPageData>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly park = computed(() => this.screenStateStore.data()?.mapItems.park ?? null);
  public readonly parkImageId = computed(() => this.screenStateStore.data()?.parkImageId ?? null);
  public readonly map: Signal<ParkItemsMapViewModel | null> = computed(() => {
    const data: ParkMapPageData | undefined = this.screenStateStore.data();
    if (!data) {
      return null;
    }

    return mapParkMapItemsToViewModel(data.mapItems, this.currentLanguageSignal());
  });

  constructor(
    @Inject(PARK_MAP_PARKS_PORT) private readonly parksPort: ParkMapParksPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadParkMap(parkId: string, closedFilter: ClosedEntityFilter = DEFAULT_CLOSED_ENTITY_FILTER): void {
    const previousData: ParkMapPageData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      mapItems: this.parksPort.getParkMapItems(parkId, { ...anonymousHttpOptions(), closedFilter }),
      summary: this.parksPort.getParkDetailSummary(parkId, { ...anonymousHttpOptions(), closedFilter })
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data: { mapItems: ParkMapItems; summary: ParkDetailSummary }) => {
        this.screenStateStore.setReady({
          mapItems: data.mapItems,
          parkImageId: resolveParkSummarySocialImageId(data.summary)
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park map items', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parks.mapPage.errorMessage', previousData);
      }
    });
  }
}
