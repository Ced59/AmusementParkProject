import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkFitSearchOrigin } from '../models/park-fit-search-form.models';
import {
  PARK_FIT_BROWSER_LOCATION_DATA_PORT,
  ParkFitBrowserLocationDataPort
} from './park-fit-browser-location-data.port';

export type ParkFitBrowserLocationStatus = 'idle' | 'loading' | 'ready' | 'error';

@Injectable()
export class ParkFitBrowserLocationFacade {
  private readonly statusSignal = signal<ParkFitBrowserLocationStatus>('idle');
  private readonly positionSignal = signal<ParkFitSearchOrigin | null>(null);
  private readonly errorKeySignal = signal<string | null>(null);

  readonly status: Signal<ParkFitBrowserLocationStatus> = this.statusSignal.asReadonly();
  readonly position: Signal<ParkFitSearchOrigin | null> = this.positionSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(PARK_FIT_BROWSER_LOCATION_DATA_PORT)
    private readonly dataPort: ParkFitBrowserLocationDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  request(): void {
    if (this.statusSignal() === 'loading') {
      return;
    }

    this.statusSignal.set('loading');
    this.errorKeySignal.set(null);
    this.dataPort.requestCurrentPosition()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (position: ParkFitSearchOrigin): void => {
          this.positionSignal.set(position);
          this.statusSignal.set('ready');
        },
        error: (error: unknown): void => {
          this.positionSignal.set(null);
          this.errorKeySignal.set(resolveLocationErrorKey(error));
          this.statusSignal.set('error');
        }
      });
  }

  clear(): void {
    this.positionSignal.set(null);
    this.errorKeySignal.set(null);
    this.statusSignal.set('idle');
  }
}

function resolveLocationErrorKey(error: unknown): string {
  return error === 'denied'
    ? 'parkFit.location.errors.denied'
    : error === 'timeout'
      ? 'parkFit.location.errors.timeout'
      : error === 'unsupported'
        ? 'parkFit.location.errors.unsupported'
        : 'parkFit.location.errors.unavailable';
}
