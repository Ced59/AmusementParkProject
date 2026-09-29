import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { StandaloneAttraction } from '@app/models/standalone-attractions/standalone-attraction';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import {
  STANDALONE_ATTRACTION_DETAIL_MANUFACTURERS_PORT,
  StandaloneAttractionDetailManufacturersPort
} from './standalone-attraction-detail-data.ports';

@Injectable()
export class StandaloneAttractionDetailReferenceFacade {
  private readonly manufacturerNameSignal = signal<string | null>(null);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly manufacturersPort: StandaloneAttractionDetailManufacturersPort = inject(
    STANDALONE_ATTRACTION_DETAIL_MANUFACTURERS_PORT
  );
  private requestedAttractionId: string | null = null;

  readonly manufacturerName = this.manufacturerNameSignal.asReadonly();

  loadManufacturer(attraction: StandaloneAttraction): void {
    const attractionId: string = attraction.id?.trim() ?? '';
    const manufacturerId: string = attraction.attractionDetails?.manufacturerId?.trim() ?? '';
    this.requestedAttractionId = attractionId || null;
    this.manufacturerNameSignal.set(null);

    if (attractionId.length === 0 || manufacturerId.length === 0) {
      return;
    }

    this.manufacturersPort.getAttractionManufacturerById(manufacturerId, false, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (manufacturer: { name?: string | null }) => {
          if (this.requestedAttractionId === attractionId) {
            this.manufacturerNameSignal.set(manufacturer.name?.trim() || null);
          }
        },
        error: () => {
          if (this.requestedAttractionId === attractionId) {
            this.manufacturerNameSignal.set(null);
          }
        }
      });
  }
}
