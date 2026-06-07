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
  private static readonly cacheTtlMs: number = 5 * 60 * 1000;
  private static readonly cachedZonesByKey: Map<string, { expiresAt: number; zones: AdminParkItemZoneOption[] }> = new Map<string, { expiresAt: number; zones: AdminParkItemZoneOption[] }>();

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly zonesSignal = signal<AdminParkItemZoneOption[]>([]);

  public readonly zones: Signal<AdminParkItemZoneOption[]> = this.zonesSignal.asReadonly();

  constructor(@Inject(ADMIN_PARK_ITEM_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT) private readonly parkZonesApiService: AdminParkItemZonesStateParkZonesApiServicePort) {
  }

  load(parkId: string, currentLanguage: string): void {
    const cacheKey: string = this.buildCacheKey(parkId, currentLanguage);
    const cachedZones: { expiresAt: number; zones: AdminParkItemZoneOption[] } | undefined = AdminParkItemZonesStateFacade.cachedZonesByKey.get(cacheKey);

    if (cachedZones && cachedZones.expiresAt > Date.now()) {
      this.zonesSignal.set(cachedZones.zones);
      return;
    }

    this.parkZonesApiService.getParkZonesByParkId(parkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (zones: ParkZone[]) => {
          const options: AdminParkItemZoneOption[] = zones
            .filter((zone: ParkZone) => !!zone.id)
            .map((zone: ParkZone) => ({
              id: zone.id ?? '',
              label: resolveLocalizedValue(zone.names, currentLanguage) ?? zone.name ?? zone.id ?? ''
            }));

          AdminParkItemZonesStateFacade.cachedZonesByKey.set(cacheKey, {
            expiresAt: Date.now() + AdminParkItemZonesStateFacade.cacheTtlMs,
            zones: options
          });
          this.zonesSignal.set(options);
        },
        error: (error: unknown) => {
          console.error('Error loading park item zones', error);
          this.zonesSignal.set([]);
        }
      });
  }

  invalidateCache(parkId: string | null = null): void {
    if (!parkId) {
      AdminParkItemZonesStateFacade.cachedZonesByKey.clear();
      return;
    }

    for (const cacheKey of AdminParkItemZonesStateFacade.cachedZonesByKey.keys()) {
      if (cacheKey.startsWith(`${parkId}|`)) {
        AdminParkItemZonesStateFacade.cachedZonesByKey.delete(cacheKey);
      }
    }
  }

  private buildCacheKey(parkId: string, currentLanguage: string): string {
    return `${parkId}|${currentLanguage}`;
  }
}
