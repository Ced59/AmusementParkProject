import { DestroyRef, Injectable, Signal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { resolveLocalizedValue } from '@shared/utils/localization';
import { ParkZone } from '@app/models/parks/park-zone';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';

export interface AdminParkItemZoneOption {
  id: string;
  label: string;
}

@Injectable()
export class AdminParkItemZonesStateFacade {
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly zonesSignal = signal<AdminParkItemZoneOption[]>([]);

  public readonly zones: Signal<AdminParkItemZoneOption[]> = this.zonesSignal.asReadonly();

  constructor(private readonly parkZonesApiService: ParkZonesApiService) {
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
