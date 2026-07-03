import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkMapItems } from '@app/models/parks/park-map-items';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { resolveParkSummarySocialImageId } from '@shared/utils/images/park-social-image.helpers';
import { resolvePublicParkItemsClosedFilter } from '@shared/utils/parks/public-park-items-closed-filter.helper';
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
  private readonly selectedClosedFilterSignal = signal<ClosedEntityFilter>(DEFAULT_CLOSED_ENTITY_FILTER);

  public readonly state = this.screenStateStore.state;
  public readonly park = computed(() => this.screenStateStore.data()?.mapItems.park ?? null);
  public readonly parkImageId = computed(() => this.screenStateStore.data()?.parkImageId ?? null);
  public readonly selectedClosedFilter = this.selectedClosedFilterSignal.asReadonly();
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
    const requestedClosedFilter: ClosedEntityFilter = closedFilter;
    this.selectedClosedFilterSignal.set(requestedClosedFilter);
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      mapItems: this.parksPort.getParkMapItems(parkId, { ...anonymousHttpOptions(), closedFilter: requestedClosedFilter }),
      summary: this.parksPort.getParkDetailSummary(parkId, { ...anonymousHttpOptions(), closedFilter: requestedClosedFilter })
    }).pipe(
      switchMap((data: { mapItems: ParkMapItems; summary: ParkDetailSummary }) => {
        const effectiveClosedFilter: ClosedEntityFilter = resolvePublicParkItemsClosedFilter(data.summary.park, requestedClosedFilter);
        this.selectedClosedFilterSignal.set(effectiveClosedFilter);

        if (effectiveClosedFilter === requestedClosedFilter) {
          return of(data);
        }

        return forkJoin({
          mapItems: this.parksPort.getParkMapItems(parkId, { ...anonymousHttpOptions(), closedFilter: effectiveClosedFilter }),
          summary: this.parksPort.getParkDetailSummary(parkId, { ...anonymousHttpOptions(), closedFilter: effectiveClosedFilter })
        });
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
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
