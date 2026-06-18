import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, Signal, signal } from '@angular/core';

import {
  DEFAULT_MEASUREMENT_SYSTEM,
  MeasurementSystem,
  normalizeMeasurementSystem
} from '@shared/models/measurements/measurement-system.model';
import { UserDto } from '@app/models/users/user_dto';

@Injectable({
  providedIn: 'root'
})
export class MeasurementPreferenceService {
  private readonly storageKey: string = 'amusementpark.measurement-system.v1';
  private readonly systemSignal = signal<MeasurementSystem>(DEFAULT_MEASUREMENT_SYSTEM);
  readonly preferredSystem: Signal<MeasurementSystem> = this.systemSignal.asReadonly();

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {
    this.systemSignal.set(this.readStoredPreference());
  }

  setPreferredSystem(value: string | null | undefined): void {
    const normalizedValue: MeasurementSystem = normalizeMeasurementSystem(value);
    this.systemSignal.set(normalizedValue);
    this.writeStoredPreference(normalizedValue);
  }

  syncFromUser(user: UserDto | null | undefined): void {
    if (!user) {
      return;
    }

    this.setPreferredSystem(user.preferredMeasurementSystem ?? DEFAULT_MEASUREMENT_SYSTEM);
  }

  getPreferredSystem(): MeasurementSystem {
    return this.systemSignal();
  }

  private readStoredPreference(): MeasurementSystem {
    if (!isPlatformBrowser(this.platformId)) {
      return DEFAULT_MEASUREMENT_SYSTEM;
    }

    try {
      return normalizeMeasurementSystem(window.localStorage.getItem(this.storageKey));
    } catch (_error) {
      return DEFAULT_MEASUREMENT_SYSTEM;
    }
  }

  private writeStoredPreference(value: MeasurementSystem): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    try {
      window.localStorage.setItem(this.storageKey, value);
    } catch (_error) {
      // Non-critical: metric remains the default fallback.
    }
  }
}
