import { DestroyRef, Injectable, Signal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';

@Injectable()
export class AdminParkItemManufacturersStateFacade {
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly manufacturerOptionsSignal = signal<EntitySelectOption[]>([]);
  private readonly manufacturersLoadingSignal = signal(false);

  public readonly manufacturerOptions: Signal<EntitySelectOption[]> = this.manufacturerOptionsSignal.asReadonly();
  public readonly manufacturersLoading: Signal<boolean> = this.manufacturersLoadingSignal.asReadonly();

  constructor(private readonly manufacturersApiService: ManufacturersApiService) {
  }

  load(): void {
    this.manufacturersLoadingSignal.set(true);

    this.manufacturersApiService.getAttractionManufacturers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (manufacturers: AttractionManufacturer[]) => {
          this.manufacturerOptionsSignal.set(
            manufacturers
              .filter((manufacturer: AttractionManufacturer) => !!manufacturer.id)
              .map((manufacturer: AttractionManufacturer) => ({
                id: manufacturer.id ?? '',
                label: manufacturer.name
              }))
          );
          this.manufacturersLoadingSignal.set(false);
        },
        error: (error: unknown) => {
          console.error('Error loading manufacturers', error);
          this.manufacturersLoadingSignal.set(false);
        }
      });
  }
}
