import { DestroyRef, Inject, Injectable, Signal, WritableSignal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import {
  CreateLiveTargetMappingCandidateRequest,
  LiveTargetMapping,
  LiveTargetMappingPage,
  LiveTargetMappingQuery,
  ReviewLiveTargetMappingRequest
} from '@app/models/admin/live-data/live-target-mapping.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_LIVE_TARGET_MAPPINGS_DATA_PORT,
  AdminLiveTargetMappingsDataPort
} from './admin-live-target-mappings-data.port';

@Injectable()
export class AdminLiveTargetMappingsFacade {
  private readonly store = new SignalScreenStateStore<LiveTargetMappingPage>();
  private readonly mutationPendingSignal: WritableSignal<boolean> = signal(false);
  private readonly feedbackKeySignal: WritableSignal<string | null> = signal(null);
  private readonly mutationErrorKeySignal: WritableSignal<string | null> = signal(null);
  private generation: number = 0;
  private loadSubscription: Subscription | null = null;

  readonly state = this.store.state;
  readonly mappings: Signal<readonly LiveTargetMapping[]> = computed(
    (): readonly LiveTargetMapping[] => this.store.data()?.data ?? []
  );
  readonly pagination = computed(() => this.store.data()?.pagination ?? null);
  readonly mutationPending = this.mutationPendingSignal.asReadonly();
  readonly feedbackKey = this.feedbackKeySignal.asReadonly();
  readonly mutationErrorKey = this.mutationErrorKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_LIVE_TARGET_MAPPINGS_DATA_PORT)
    private readonly dataPort: AdminLiveTargetMappingsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(query: LiveTargetMappingQuery = {}): void {
    const generation: number = ++this.generation;
    const previous: LiveTargetMappingPage | undefined = this.store.data();
    this.store.setLoading(previous);
    this.loadSubscription?.unsubscribe();
    this.loadSubscription = this.dataPort.search(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page: LiveTargetMappingPage): void => {
          if (generation === this.generation) {
            if (page.data.length === 0) {
              this.store.setEmpty(page);
            } else {
              this.store.setReady(page);
            }
          }
        },
        error: (): void => {
          if (generation === this.generation) {
            this.store.setError('admin.liveMappings.errors.load', previous);
          }
        }
      });
  }

  createCandidate(
    request: CreateLiveTargetMappingCandidateRequest,
    refreshQuery: LiveTargetMappingQuery
  ): void {
    this.mutate(
      this.dataPort.createCandidate(request),
      'admin.liveMappings.messages.created',
      refreshQuery
    );
  }

  review(
    mappingId: string,
    request: ReviewLiveTargetMappingRequest,
    refreshQuery: LiveTargetMappingQuery
  ): void {
    this.mutate(
      this.dataPort.review(mappingId, request),
      'admin.liveMappings.messages.reviewed',
      refreshQuery
    );
  }

  clearFeedback(): void {
    this.feedbackKeySignal.set(null);
    this.mutationErrorKeySignal.set(null);
  }

  private mutate(
    operation: import('rxjs').Observable<LiveTargetMapping>,
    successKey: string,
    refreshQuery: LiveTargetMappingQuery
  ): void {
    if (this.mutationPendingSignal()) {
      return;
    }

    this.mutationPendingSignal.set(true);
    this.clearFeedback();
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (): void => {
        this.mutationPendingSignal.set(false);
        this.feedbackKeySignal.set(successKey);
        this.load(refreshQuery);
      },
      error: (): void => {
        this.mutationPendingSignal.set(false);
        this.mutationErrorKeySignal.set('admin.liveMappings.errors.mutation');
      }
    });
  }
}
