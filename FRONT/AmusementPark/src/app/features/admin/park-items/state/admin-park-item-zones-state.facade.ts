import {
  DestroyRef,
  Injectable,
  Signal,
  inject,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { resolveLocalizedValue } from '@shared/utils/localization';
import { ParkZone } from '@app/models/parks/park-zone';

import {
  ADMIN_PARK_ITEM_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT,
  AdminParkItemZonesStateParkZonesApiServicePort
} from './admin-park-item-zones-state-data.ports';
export interface AdminParkItemZoneOption {
  id: string;
  label: string;
}

@Injectable()
export class AdminParkItemZonesStateFacade {
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly zonesSignal = signal<AdminParkItemZoneOption[]>([]);

  public readonly zones: Signal<AdminParkItemZoneOption[]> = this.zonesSignal.asReadonly();

  constructor(@Inject(ADMIN_PARK_ITEM_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT) private readonly parkZonesApiService: AdminParkItemZonesStateParkZonesApiServicePort) {
  }

  load(parkId: string, currentLanguage: string): void {
    this.parkZonesApiService.getParkZonesByParkId(parkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (zones: ParkZone[]) => {
          this.zonesSignal.set(
            zones
              .filter((zone: ParkZone) => !!zone.id)
              .map((zone: ParkZone) => ({
                id: zone.id ?? '',
                label: resolveLocalizedValue(zone.names, currentLanguage) ?? zone.name ?? zone.id ?? ''
              }))
          );
        },
        error: (error: unknown) => {
          console.error('Error loading park item zones', error);
          this.zonesSignal.set([]);
        }
      });
  }
}
