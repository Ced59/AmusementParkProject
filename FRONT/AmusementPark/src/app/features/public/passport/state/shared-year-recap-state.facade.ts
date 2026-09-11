import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SharedYearRecap } from '@app/models/sharing/share-publication.models';
import { SHARED_YEAR_RECAP_PORT, SharedYearRecapPort } from './shared-year-recap-state-data.ports';

@Injectable()
export class SharedYearRecapStateFacade {
  private readonly recapSignal = signal<SharedYearRecap | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly notFoundSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private requestGeneration: number = 0;

  public readonly recap: Signal<SharedYearRecap | null> = this.recapSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(SHARED_YEAR_RECAP_PORT) private readonly recapPort: SharedYearRecapPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(shareId: string): void {
    const generation: number = ++this.requestGeneration;
    this.recapSignal.set(null);
    this.loadingSignal.set(true);
    this.notFoundSignal.set(false);
    this.errorSignal.set(false);
    this.recapPort.getSharedYear(shareId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (recap: SharedYearRecap): void => {
        if (generation !== this.requestGeneration) {
          return;
        }
        this.recapSignal.set(recap);
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
