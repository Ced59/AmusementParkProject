import { Injectable, Signal, computed } from '@angular/core';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { ParkZone } from '@app/models/parks/park-zone';

interface AdminParkZonesViewModel {
  zones: ParkZone[];
}

@Injectable()
export class AdminParkZonesStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminParkZonesViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly zones: Signal<ParkZone[]> = computed(() => this.screenStateStore.data()?.zones ?? []);

  constructor(private readonly parkZonesApiService: ParkZonesApiService) {
  }

  loadZones(parkId: string): void {
    const previousData: AdminParkZonesViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkZonesApiService.getParkZonesByParkId(parkId).subscribe({
      next: (zones: ParkZone[]) => {
        const viewModel: AdminParkZonesViewModel = {
          zones
        };

        if (zones.length === 0) {
          this.screenStateStore.setEmpty(viewModel);
          return;
        }

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading zones', error);
        this.screenStateStore.setError('common.errorMessage', previousData);
      }
    });
  }
}
