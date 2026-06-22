import {
  DestroyRef,
  Injectable,
  Signal,
  inject,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';

import {
  ADMIN_PARK_ITEM_MANUFACTURERS_STATE_MANUFACTURERS_API_SERVICE_PORT,
  AdminParkItemManufacturersStateManufacturersApiServicePort
} from './admin-park-item-manufacturers-state-data.ports';
@Injectable()
export class AdminParkItemManufacturersStateFacade {
  private static readonly cacheTtlMs: number = 5 * 60 * 1000;
  private static cachedOptions: { expiresAt: number; options: EntitySelectOption[] } | null = null;

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly manufacturerOptionsSignal = signal<EntitySelectOption[]>([]);
  private readonly manufacturersLoadingSignal = signal(false);

  public readonly manufacturerOptions: Signal<EntitySelectOption[]> = this.manufacturerOptionsSignal.asReadonly();
  public readonly manufacturersLoading: Signal<boolean> = this.manufacturersLoadingSignal.asReadonly();

  constructor(@Inject(ADMIN_PARK_ITEM_MANUFACTURERS_STATE_MANUFACTURERS_API_SERVICE_PORT) private readonly manufacturersApiService: AdminParkItemManufacturersStateManufacturersApiServicePort) {
  }

  load(): void {
    const cachedOptions: { expiresAt: number; options: EntitySelectOption[] } | null = AdminParkItemManufacturersStateFacade.cachedOptions;

    if (cachedOptions && cachedOptions.expiresAt > Date.now()) {
      this.manufacturerOptionsSignal.set(cachedOptions.options);
      return;
    }

    this.manufacturersLoadingSignal.set(true);

    this.manufacturersApiService.getAttractionManufacturers(true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (manufacturers: AttractionManufacturer[]) => {
          const options: EntitySelectOption[] = manufacturers
            .filter((manufacturer: AttractionManufacturer) => !!manufacturer.id)
            .map((manufacturer: AttractionManufacturer) => ({
              id: manufacturer.id ?? '',
              label: manufacturer.name
            }));

          AdminParkItemManufacturersStateFacade.cachedOptions = {
            expiresAt: Date.now() + AdminParkItemManufacturersStateFacade.cacheTtlMs,
            options
          };
          this.manufacturerOptionsSignal.set(options);
          this.manufacturersLoadingSignal.set(false);
        },
        error: (error: unknown) => {
          console.error('Error loading manufacturers', error);
          this.manufacturersLoadingSignal.set(false);
        }
      });
  }

  invalidateCache(): void {
    AdminParkItemManufacturersStateFacade.cachedOptions = null;
  }
}
