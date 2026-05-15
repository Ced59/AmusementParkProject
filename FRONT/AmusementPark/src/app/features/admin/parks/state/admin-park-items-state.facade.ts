import { Injectable, Signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';

interface AdminParkItemsViewModel {
  items: ParkItem[];
  zones: ParkZone[];
}

@Injectable()
export class AdminParkItemsStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminParkItemsViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly items: Signal<ParkItem[]> = computed(() => this.screenStateStore.data()?.items ?? []);
  public readonly zones: Signal<ParkZone[]> = computed(() => this.screenStateStore.data()?.zones ?? []);

  constructor(
    private readonly parkZonesApiService: ParkZonesApiService,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadData(parkId: string): void {
    const previousData: AdminParkItemsViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      zones: this.parkZonesApiService.getParkZonesByParkId(parkId),
      items: this.parkItemsApiService.getParkItemsByParkId(parkId)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ zones, items }: AdminParkItemsViewModel) => {
        const viewModel: AdminParkItemsViewModel = {
          zones,
          items
        };

        if (items.length === 0) {
          this.screenStateStore.setEmpty(viewModel);
          return;
        }

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading park items', error);
        this.screenStateStore.setError('common.errorMessage', previousData);
      }
    });
  }
}
