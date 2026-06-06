import {
  Injectable,
  Signal,
  computed,
  DestroyRef,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { ParkZone } from '@app/models/parks/park-zone';

import {
  ADMIN_PARK_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT,
  AdminParkZonesStateParkZonesApiServicePort
} from './admin-park-zones-state-data.ports';
interface AdminParkZonesViewModel {
  zones: ParkZone[];
}

@Injectable()
export class AdminParkZonesStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminParkZonesViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly zones: Signal<ParkZone[]> = computed(() => this.screenStateStore.data()?.zones ?? []);

  constructor(@Inject(ADMIN_PARK_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT) private readonly parkZonesApiService: AdminParkZonesStateParkZonesApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadZones(parkId: string): void {
    const previousData: AdminParkZonesViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkZonesApiService.getParkZonesByParkId(parkId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (zones: ParkZone[]) => {
        const viewModel: AdminParkZonesViewModel = {
          zones
        };

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading zones', error);
        this.screenStateStore.setError('common.errorMessage', previousData);
      }
    });
  }
}
