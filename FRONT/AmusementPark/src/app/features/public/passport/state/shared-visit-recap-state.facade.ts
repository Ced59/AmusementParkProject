import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SharedVisitRecap } from '@app/models/sharing/share-publication.models';
import { SHARED_VISIT_RECAP_PORT, SharedVisitRecapPort } from './shared-visit-recap-state-data.ports';

@Injectable()
export class SharedVisitRecapStateFacade {
  private readonly recapSignal = signal<SharedVisitRecap | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly notFoundSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private requestGeneration: number = 0;

  public readonly recap: Signal<SharedVisitRecap | null> = this.recapSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(SHARED_VISIT_RECAP_PORT) private readonly recapPort: SharedVisitRecapPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(shareId: string): void {
    const generation: number = ++this.requestGeneration;
    this.recapSignal.set(null);
    this.loadingSignal.set(true);
    this.notFoundSignal.set(false);
    this.errorSignal.set(false);
    this.recapPort.getSharedVisit(shareId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (recap: SharedVisitRecap): void => {
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
