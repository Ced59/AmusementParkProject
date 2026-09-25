import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { TripExport } from '@app/models/trips/trip-export.models';
import { TRIP_OPERATION_ID_PORT, TripOperationIdPort } from './trip-state-data.ports';
import { TRIP_EXPORT_DATA_PORT, TripExportDataPort } from './trip-export-data.port';

@Injectable()
export class TripExportFacade {
  private tripPlanId: string = '';
  private readonly planSignal = signal<TripExport | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);

  readonly plan: Signal<TripExport | null> = this.planSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(TRIP_EXPORT_DATA_PORT) private readonly data: TripExportDataPort,
    @Inject(TRIP_OPERATION_ID_PORT) private readonly operationIds: TripOperationIdPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(tripPlanId: string): void {
    const normalizedId: string = tripPlanId.trim();
    if (!normalizedId || this.loadingSignal()) {
      return;
    }

    this.tripPlanId = normalizedId;
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.data.get(normalizedId, this.operationIds.create()).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.loadingSignal.set(false))
    ).subscribe({
      next: (plan: TripExport): void => this.planSignal.set(plan),
      error: (): void => {
        this.planSignal.set(null);
        this.errorSignal.set(true);
      }
    });
  }

  retry(): void {
    this.load(this.tripPlanId);
  }
}
