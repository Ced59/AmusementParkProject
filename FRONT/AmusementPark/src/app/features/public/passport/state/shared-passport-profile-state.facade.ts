import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SharedPassportProfile } from '@app/models/sharing/share-publication.models';
import { SHARED_PASSPORT_PROFILE_PORT, SharedPassportProfilePort } from './shared-passport-profile-state-data.ports';

@Injectable()
export class SharedPassportProfileStateFacade {
  private readonly profileSignal = signal<SharedPassportProfile | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly notFoundSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private requestGeneration: number = 0;

  public readonly profile: Signal<SharedPassportProfile | null> = this.profileSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(SHARED_PASSPORT_PROFILE_PORT) private readonly port: SharedPassportProfilePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  public load(shareId: string): void {
    const generation: number = ++this.requestGeneration;
    this.profileSignal.set(null);
    this.loadingSignal.set(true);
    this.notFoundSignal.set(false);
    this.errorSignal.set(false);
    this.port.getSharedPassportProfile(shareId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (profile: SharedPassportProfile): void => {
        if (generation !== this.requestGeneration) {
          return;
        }
        this.profileSignal.set(profile);
        this.loadingSignal.set(false);
      },
      error: (error: { status?: number }): void => {
        if (generation !== this.requestGeneration) {
          return;
        }
        this.loadingSignal.set(false);
        this.notFoundSignal.set(error?.status === 404);
        this.errorSignal.set(error?.status !== 404);
      }
    });
  }
}
