import { DestroyRef, Injectable, Signal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroup } from '@angular/forms';

import { MapMarker } from '@app/models/map/map-marker';
import { DEFAULT_ADMIN_PARK_COORDINATES } from '../mappers/admin-park-edit-form.mapper';

@Injectable()
export class AdminParkLocationStateFacade {
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly mapCenterSignal = signal<[number, number]>(DEFAULT_ADMIN_PARK_COORDINATES);
  private readonly mapZoomSignal = signal(16);
  private readonly mapMarkersSignal = signal<MapMarker[]>([]);
  private form: FormGroup | null = null;

  public readonly mapCenter: Signal<[number, number]> = this.mapCenterSignal.asReadonly();
  public readonly mapZoom: Signal<number> = this.mapZoomSignal.asReadonly();
  public readonly mapMarkers: Signal<MapMarker[]> = this.mapMarkersSignal.asReadonly();

  bindForm(form: FormGroup): void {
    this.form = form;

    this.form.get('latitude')?.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.refreshFromForm();
      });

    this.form.get('longitude')?.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.refreshFromForm();
      });

    this.refreshFromForm();
  }

  updatePosition(position: { lat: number; lng: number }): void {
    if (!this.form) {
      return;
    }

    this.form.patchValue({
      latitude: position.lat,
      longitude: position.lng
    }, { emitEvent: true });
  }

  refreshFromForm(): void {
    if (!this.form) {
      return;
    }

    const latitude: number = Number(this.form.get('latitude')?.value);
    const longitude: number = Number(this.form.get('longitude')?.value);

    if (Number.isNaN(latitude) || Number.isNaN(longitude)) {
      return;
    }

    this.mapCenterSignal.set([latitude, longitude]);
    this.mapMarkersSignal.set([
      {
        id: 'park-marker',
        lat: latitude,
        lng: longitude,
        draggable: true,
        iconKind: 'park'
      }
    ]);
  }
}
