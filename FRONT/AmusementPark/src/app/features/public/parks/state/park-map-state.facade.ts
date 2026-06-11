import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkMapItems } from '@app/models/parks/park-map-items';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapParkMapItemsToViewModel } from '../mappers/park-map-items-view.mapper';
import { ParkItemsMapViewModel } from '../models/park-items-map-view.model';
import { PARK_MAP_PARKS_PORT, ParkMapParksPort } from './park-map-data.ports';

@Injectable()
export class ParkMapStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkMapItems>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly park = computed(() => this.screenStateStore.data()?.park ?? null);
  public readonly map: Signal<ParkItemsMapViewModel | null> = computed(() => {
    const data: ParkMapItems | undefined = this.screenStateStore.data();
    if (!data) {
      return null;
    }

    return mapParkMapItemsToViewModel(data, this.currentLanguageSignal());
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

  loadParkMap(parkId: string): void {
    const previousData: ParkMapItems | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parksPort.getParkMapItems(parkId, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data: ParkMapItems) => {
        this.screenStateStore.setReady(data);
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
