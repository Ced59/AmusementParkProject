import { DestroyRef, Injectable, Signal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroup } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { MapMarker } from '@app/models/map/map-marker';
import { Park } from '@app/models/parks/park';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { AttractionLocationKey, ParkCoordinates } from '../models/admin-park-item-edit.model';

@Injectable()
export class AdminParkItemLocationStateFacade {
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly selectedLocationKeySignal = signal<AttractionLocationKey>('entrance');
  private readonly generalMapCenterSignal = signal<[number, number]>([48.8566, 2.3522]);
  private readonly generalMapMarkersSignal = signal<MapMarker[]>([]);
  private readonly locationMapCenterSignal = signal<[number, number]>([48.8566, 2.3522]);
  private readonly locationMapMarkersSignal = signal<MapMarker[]>([]);
  private readonly parkLocationDefaultSignal = signal<ParkCoordinates | null>(null);

  private form: FormGroup | null = null;
  private generalLocationManuallyChanged: boolean = false;
  private isApplyingGeneralLocationProgrammatically: boolean = false;

  public readonly generalMapZoom: number = 18;
  public readonly locationMapZoom: number = 19;
  public readonly selectedLocationKey: Signal<AttractionLocationKey> = this.selectedLocationKeySignal.asReadonly();
  public readonly generalMapCenter: Signal<[number, number]> = this.generalMapCenterSignal.asReadonly();
  public readonly generalMapMarkers: Signal<MapMarker[]> = this.generalMapMarkersSignal.asReadonly();
  public readonly locationMapCenter: Signal<[number, number]> = this.locationMapCenterSignal.asReadonly();
  public readonly locationMapMarkers: Signal<MapMarker[]> = this.locationMapMarkersSignal.asReadonly();
  public readonly canUseParkLocation: Signal<boolean> = computed(() => this.parkLocationDefaultSignal() !== null);

  constructor(private readonly parksApiService: ParksApiService) {
  }

  bindForm(form: FormGroup): void {
    this.form = form;

    form.get('latitude')?.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        if (!this.isApplyingGeneralLocationProgrammatically) {
          this.generalLocationManuallyChanged = true;
        }

        this.updateGeneralMapState();
        this.updateLocationMapState();
      });

    form.get('longitude')?.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        if (!this.isApplyingGeneralLocationProgrammatically) {
          this.generalLocationManuallyChanged = true;
        }

        this.updateGeneralMapState();
        this.updateLocationMapState();
      });

    form.get('attractionLocations')?.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        this.updateLocationMapState();
      });
  }

  async loadParkLocationDefaultAsync(parkId: string, applyToGeneralLocation: boolean): Promise<void> {
    if (!parkId) {
      return;
    }

    const park: Park = await firstValueFrom(this.parksApiService.getParkById(parkId));
    this.parkLocationDefaultSignal.set({
      latitude: park.latitude,
      longitude: park.longitude
    });

    if (applyToGeneralLocation) {
      this.generalLocationManuallyChanged = false;
      this.applyGeneralLocation(park.latitude, park.longitude);
      return;
    }

    this.refreshFromForm();
  }

  markGeneralLocationAsManuallyChanged(): void {
    this.generalLocationManuallyChanged = true;
  }

  updateGeneralPosition(position: { lat: number; lng: number }): void {
    if (!this.form) {
      return;
    }

    this.generalLocationManuallyChanged = true;
    this.form.patchValue({
      latitude: position.lat,
      longitude: position.lng
    });
  }

  resetGeneralLocationToPark(): void {
    const parkLocation: ParkCoordinates | null = this.getResolvedParkLocationDefault();

    if (!parkLocation) {
      return;
    }

    this.generalLocationManuallyChanged = false;
    this.applyGeneralLocation(parkLocation.latitude, parkLocation.longitude);
  }

  selectLocationEditor(locationKey: AttractionLocationKey): void {
    this.selectedLocationKeySignal.set(locationKey);
    this.updateLocationMapState();
  }

  updateSpecificLocation(position: { lat: number; lng: number }): void {
    const group: FormGroup | null = this.getLocationGroup(this.selectedLocationKeySignal());

    if (!group) {
      return;
    }

    group.patchValue({
      latitude: position.lat,
      longitude: position.lng
    });
  }

  clearLocationPoint(locationKey: AttractionLocationKey): void {
    const group: FormGroup | null = this.getLocationGroup(locationKey);

    if (!group) {
      return;
    }

    group.patchValue({
      latitude: null,
      longitude: null
    });
  }

  clearSelectedLocationPoint(): void {
    this.clearLocationPoint(this.selectedLocationKeySignal());
  }

  useGeneralLocationForSelectedPoint(): void {
    if (!this.form) {
      return;
    }

    const latitude: number = this.toRequiredNumber(this.form.get('latitude')?.value);
    const longitude: number = this.toRequiredNumber(this.form.get('longitude')?.value);
    const group: FormGroup | null = this.getLocationGroup(this.selectedLocationKeySignal());

    group?.patchValue({
      latitude,
      longitude
    });
  }

  refreshFromForm(): void {
    this.updateGeneralMapState();
    this.updateLocationMapState();
  }

  private applyGeneralLocation(latitude: number, longitude: number): void {
    if (!this.form) {
      return;
    }

    this.isApplyingGeneralLocationProgrammatically = true;
    this.form.patchValue({
      latitude,
      longitude
    }, { emitEvent: false });
    this.isApplyingGeneralLocationProgrammatically = false;
    this.refreshFromForm();
  }

  private updateGeneralMapState(): void {
    if (!this.form) {
      return;
    }

    const latitude: number = this.toRequiredNumber(this.form.get('latitude')?.value);
    const longitude: number = this.toRequiredNumber(this.form.get('longitude')?.value);
    const hasValidCoordinates: boolean = Number.isFinite(latitude) && Number.isFinite(longitude) && !(latitude === 0 && longitude === 0);
    const parkLocation: ParkCoordinates | null = this.getResolvedParkLocationDefault();

    if (!hasValidCoordinates && parkLocation) {
      this.generalMapCenterSignal.set([parkLocation.latitude, parkLocation.longitude]);
      this.generalMapMarkersSignal.set([
        {
          id: 'general-location-default',
          lat: parkLocation.latitude,
          lng: parkLocation.longitude
        }
      ]);
      return;
    }

    this.generalMapCenterSignal.set([latitude, longitude]);
    this.generalMapMarkersSignal.set([
      {
        id: 'general-location',
        lat: latitude,
        lng: longitude
      }
    ]);
  }

  private updateLocationMapState(): void {
    if (!this.form) {
      return;
    }

    const point: AttractionLocationPoint | null = this.getLocationPoint(this.selectedLocationKeySignal());

    if (point && point.latitude !== null && point.longitude !== null && point.latitude !== undefined && point.longitude !== undefined) {
      this.locationMapCenterSignal.set([point.latitude, point.longitude]);
      this.locationMapMarkersSignal.set([
        {
          id: this.selectedLocationKeySignal(),
          lat: point.latitude,
          lng: point.longitude
        }
      ]);
      return;
    }

    const latitude: number = this.toRequiredNumber(this.form.get('latitude')?.value);
    const longitude: number = this.toRequiredNumber(this.form.get('longitude')?.value);
    const hasGeneralCoordinates: boolean = Number.isFinite(latitude) && Number.isFinite(longitude) && !(latitude === 0 && longitude === 0);
    const parkLocation: ParkCoordinates | null = this.getResolvedParkLocationDefault();

    if (!hasGeneralCoordinates && parkLocation) {
      this.locationMapCenterSignal.set([parkLocation.latitude, parkLocation.longitude]);
      this.locationMapMarkersSignal.set([]);
      return;
    }

    this.locationMapCenterSignal.set([latitude, longitude]);
    this.locationMapMarkersSignal.set([]);
  }

  private getResolvedParkLocationDefault(): ParkCoordinates | null {
    const location: ParkCoordinates | null = this.parkLocationDefaultSignal();

    if (!location) {
      return null;
    }

    if (!Number.isFinite(location.latitude) || !Number.isFinite(location.longitude)) {
      return null;
    }

    return location;
  }

  private getLocationGroup(locationKey: AttractionLocationKey): FormGroup | null {
    return this.form?.get(['attractionLocations', locationKey]) as FormGroup | null;
  }

  private getLocationPoint(locationKey: AttractionLocationKey): AttractionLocationPoint | null {
    const group: FormGroup | null = this.getLocationGroup(locationKey);
    const groupValue: { latitude?: unknown; longitude?: unknown } | undefined = group?.getRawValue() as { latitude?: unknown; longitude?: unknown } | undefined;
    const latitude: number | null = this.toNullableNumber(groupValue?.latitude);
    const longitude: number | null = this.toNullableNumber(groupValue?.longitude);

    if (latitude === null || longitude === null) {
      return null;
    }

    return {
      latitude,
      longitude
    };
  }

  private toNullableNumber(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const parsed: number = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  private toRequiredNumber(value: unknown): number {
    const parsed: number = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }
}
