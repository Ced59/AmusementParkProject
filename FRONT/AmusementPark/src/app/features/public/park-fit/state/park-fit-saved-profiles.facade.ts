import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import {
  PARK_FIT_SAVED_PROFILES_DATA_PORT,
  ParkFitSavedProfilesDataPort
} from './park-fit-saved-profiles-data.port';

export type ParkFitSavedProfilesStatus = 'idle' | 'loading' | 'success' | 'error';

@Injectable()
export class ParkFitSavedProfilesFacade {
  private readonly profilesSignal = signal<ParkFitGroupProfile[]>([]);
  private readonly statusSignal = signal<ParkFitSavedProfilesStatus>('idle');

  public readonly profiles: Signal<ParkFitGroupProfile[]> = this.profilesSignal.asReadonly();
  public readonly status: Signal<ParkFitSavedProfilesStatus> = this.statusSignal.asReadonly();

  constructor(
    @Inject(PARK_FIT_SAVED_PROFILES_DATA_PORT)
    private readonly dataPort: ParkFitSavedProfilesDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    if (this.statusSignal() === 'loading') {
      return;
    }

    this.statusSignal.set('loading');
    this.dataPort.listMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profiles: ParkFitGroupProfile[]): void => {
          this.profilesSignal.set(profiles);
          this.statusSignal.set('success');
        },
        error: (): void => {
          this.profilesSignal.set([]);
          this.statusSignal.set('error');
        }
      });
  }
}
