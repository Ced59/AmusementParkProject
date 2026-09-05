import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, Observable, take } from 'rxjs';

import {
  CreatePassportRideOccurrencesBatchRequest,
  PassportRideOccurrence,
  PassportRideOccurrenceMutationResult,
  PassportVisitRideTargetEvaluation
} from '@app/models/passport/passport-ride-occurrence.models';
import { PassportVisit, PassportVisitPage } from '@app/models/passport/passport-visit.models';
import { AuthService } from '@app/services/auth/auth.service';
import { extractApiProblemDetails } from '@shared/utils/security/error-display.helpers';
import {
  isParkItemRideRatingValid,
  mapParkItemRideDraftToRequest,
  mapPassportVisitToParkItemRideVisitOption
} from '../mappers/park-item-passport-ride.mapper';
import {
  ParkItemPassportRideDraft,
  ParkItemPassportRideEvaluation,
  ParkItemPassportRideOutcome,
  ParkItemPassportRideTarget,
  ParkItemPassportRideVisitOption
} from '../models/park-item-passport-ride.models';
import {
  PARK_ITEM_PASSPORT_RIDE_OCCURRENCES_PORT,
  PARK_ITEM_PASSPORT_RIDE_OPERATION_ID_PORT,
  PARK_ITEM_PASSPORT_RIDE_VISITS_PORT,
  ParkItemPassportRideOccurrencesPort,
  ParkItemPassportRideOperationIdPort,
  ParkItemPassportRideVisitsPort
} from './park-item-passport-ride-state-data.ports';

const visitPageSize: number = 20;

interface PendingRideSubmission {
  fingerprint: string;
  idempotencyKey: string;
}

@Injectable()
export class ParkItemPassportRideStateFacade {
  private readonly targetSignal = signal<ParkItemPassportRideTarget | null>(null);
  private readonly visitsSignal = signal<PassportVisit[]>([]);
  private readonly selectedVisitIdSignal = signal<string | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadingMoreSignal = signal<boolean>(false);
  private readonly evaluatingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly nextCursorSignal = signal<string | null>(null);
  private readonly evaluationSignal = signal<ParkItemPassportRideEvaluation | null>(null);
  private readonly outcomeSignal = signal<ParkItemPassportRideOutcome | null>(null);
  private readonly createdVisitIdSignal = signal<string | null>(null);
  private readonly addedCountSignal = signal<number>(0);
  private configurationGeneration: number = 0;
  private selectionGeneration: number = 0;
  private pendingSubmission: PendingRideSubmission | null = null;

  readonly isAuthenticated: Signal<boolean> = computed((): boolean => this.authService.isLoggedIn());
  readonly visits: Signal<ParkItemPassportRideVisitOption[]> = computed((): ParkItemPassportRideVisitOption[] => {
    const language: string = this.targetSignal()?.language ?? 'en';
    return this.visitsSignal()
      .map((visit: PassportVisit): ParkItemPassportRideVisitOption | null =>
        mapPassportVisitToParkItemRideVisitOption(visit, language))
      .filter((visit: ParkItemPassportRideVisitOption | null): visit is ParkItemPassportRideVisitOption => visit !== null);
  });
  readonly selectedVisitId: Signal<string | null> = this.selectedVisitIdSignal.asReadonly();
  readonly selectedVisit: Signal<ParkItemPassportRideVisitOption | null> = computed(() =>
    this.visits().find((visit: ParkItemPassportRideVisitOption): boolean =>
      visit.id === this.selectedVisitIdSignal()) ?? null);
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  readonly evaluating: Signal<boolean> = this.evaluatingSignal.asReadonly();
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  readonly evaluation: Signal<ParkItemPassportRideEvaluation | null> = this.evaluationSignal.asReadonly();
  readonly outcome: Signal<ParkItemPassportRideOutcome | null> = this.outcomeSignal.asReadonly();
  readonly createdVisitId: Signal<string | null> = this.createdVisitIdSignal.asReadonly();
  readonly addedCount: Signal<number> = this.addedCountSignal.asReadonly();
  readonly hasMore: Signal<boolean> = computed((): boolean => this.nextCursorSignal() !== null);
  readonly isEmpty: Signal<boolean> = computed((): boolean =>
    !this.loadingSignal() && this.errorKeySignal() === null && this.visits().length === 0);

  constructor(
    @Inject(PARK_ITEM_PASSPORT_RIDE_VISITS_PORT)
    private readonly visitsApi: ParkItemPassportRideVisitsPort,
    @Inject(PARK_ITEM_PASSPORT_RIDE_OCCURRENCES_PORT)
    private readonly occurrencesApi: ParkItemPassportRideOccurrencesPort,
    @Inject(PARK_ITEM_PASSPORT_RIDE_OPERATION_ID_PORT)
    private readonly operationIds: ParkItemPassportRideOperationIdPort,
    private readonly authService: AuthService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  configure(target: ParkItemPassportRideTarget): void {
    const normalizedTarget: ParkItemPassportRideTarget | null = normalizeTarget(target);
    const previous: ParkItemPassportRideTarget | null = this.targetSignal();
    if (!normalizedTarget) {
      this.reset(null);
      return;
    }

    const targetChanged: boolean = previous?.parkId !== normalizedTarget.parkId
      || previous?.parkItemId !== normalizedTarget.parkItemId;
    this.targetSignal.set(normalizedTarget);
    if (targetChanged) {
      this.reset(normalizedTarget);
    }
  }

  load(): void {
    const target: ParkItemPassportRideTarget | null = this.targetSignal();
    if (!target || !this.authService.isLoggedIn() || this.loadingSignal()) {
      return;
    }

    const generation: number = ++this.configurationGeneration;
    this.loadingSignal.set(true);
    this.loadingMoreSignal.set(false);
    this.errorKeySignal.set(null);
    this.outcomeSignal.set(null);
    this.visitsApi.listVisits(visitPageSize, null, { parkId: target.parkId, status: 'Draft' })
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page: PassportVisitPage): void => {
          if (!this.isCurrentConfiguration(generation, target)) {
            return;
          }

          this.visitsSignal.set(this.filterCompatibleVisits(page.items, target.parkId));
          this.nextCursorSignal.set(normalizeCursor(page.nextCursor));
          this.loadingSignal.set(false);
        },
        error: (error: unknown): void => {
          if (!this.isCurrentConfiguration(generation, target)) {
            return;
          }

          console.error('Error loading draft visits for park item ride logging', error);
          this.visitsSignal.set([]);
          this.nextCursorSignal.set(null);
          this.loadingSignal.set(false);
          this.errorKeySignal.set('parkItems.passportRide.errors.loadVisits');
        }
      });
  }

  loadMore(): void {
    const target: ParkItemPassportRideTarget | null = this.targetSignal();
    const cursor: string | null = this.nextCursorSignal();
    if (!target || !cursor || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    const generation: number = this.configurationGeneration;
    this.loadingMoreSignal.set(true);
    this.errorKeySignal.set(null);
    this.visitsApi.listVisits(visitPageSize, cursor, { parkId: target.parkId, status: 'Draft' })
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page: PassportVisitPage): void => {
          if (!this.isCurrentConfiguration(generation, target)) {
            return;
          }

          this.visitsSignal.set(deduplicateVisits([
            ...this.visitsSignal(),
            ...this.filterCompatibleVisits(page.items, target.parkId)
          ]));
          this.nextCursorSignal.set(normalizeCursor(page.nextCursor));
          this.loadingMoreSignal.set(false);
        },
        error: (error: unknown): void => {
          if (!this.isCurrentConfiguration(generation, target)) {
            return;
          }

          console.error('Error loading more draft visits for park item ride logging', error);
          this.loadingMoreSignal.set(false);
          this.errorKeySignal.set('parkItems.passportRide.errors.loadMoreVisits');
        }
      });
  }

  selectVisit(visitId: string): void {
    const normalizedVisitId: string = visitId.trim();
    const target: ParkItemPassportRideTarget | null = this.targetSignal();
    const visitExists: boolean = this.visitsSignal().some((visit: PassportVisit): boolean =>
      visit.id === normalizedVisitId && visit.status === 'Draft' && visit.parkId === target?.parkId);
    if (!target || !visitExists || this.savingSignal()) {
      return;
    }

    this.selectedVisitIdSignal.set(normalizedVisitId);
    this.evaluationSignal.set(null);
    this.outcomeSignal.set(null);
    this.errorKeySignal.set(null);
    const selectionGeneration: number = ++this.selectionGeneration;
    this.evaluatingSignal.set(true);
    this.occurrencesApi.evaluateVisitTargets(normalizedVisitId, [target.parkItemId])
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (evaluations: PassportVisitRideTargetEvaluation[]): void => {
          if (!this.isCurrentSelection(selectionGeneration, normalizedVisitId, target)) {
            return;
          }

          const evaluation: PassportVisitRideTargetEvaluation | undefined = evaluations.find(
            (candidate: PassportVisitRideTargetEvaluation): boolean =>
              candidate.parkItemId === target.parkItemId);
          this.evaluatingSignal.set(false);
          if (!evaluation) {
            this.errorKeySignal.set('parkItems.passportRide.errors.evaluate');
            return;
          }

          this.evaluationSignal.set({
            consistency: evaluation.historicalConsistency,
            openingDate: evaluation.openingDate,
            closingDate: evaluation.closingDate
          });
        },
        error: (error: unknown): void => {
          if (!this.isCurrentSelection(selectionGeneration, normalizedVisitId, target)) {
            return;
          }

          console.error('Error evaluating a park item for a selected visit', error);
          this.evaluatingSignal.set(false);
          this.errorKeySignal.set('parkItems.passportRide.errors.evaluate');
        }
      });
  }

  addCreatedVisit(visit: PassportVisit): void {
    const target: ParkItemPassportRideTarget | null = this.targetSignal();
    if (!target || visit.parkId !== target.parkId || visit.status !== 'Draft') {
      return;
    }

    this.visitsSignal.set(deduplicateVisits([visit, ...this.visitsSignal()]));
    this.selectVisit(visit.id);
  }

  addRide(draft: ParkItemPassportRideDraft): void {
    if (this.outcomeSignal() !== null) {
      return;
    }

    const target: ParkItemPassportRideTarget | null = this.targetSignal();
    const selectedVisit: ParkItemPassportRideVisitOption | null = this.selectedVisit();
    const evaluation: ParkItemPassportRideEvaluation | null = this.evaluationSignal();
    if (!target || !selectedVisit || draft.visitId !== selectedVisit.id || !evaluation || this.savingSignal()) {
      this.errorKeySignal.set('parkItems.passportRide.errors.incomplete');
      return;
    }

    if (evaluation.consistency === 'ConfirmedConflict' && !draft.confirmHistoricalConflict) {
      this.errorKeySignal.set('parkItems.passportRide.errors.confirmConflict');
      return;
    }

    if (!isParkItemRideRatingValid(draft.rating)) {
      this.errorKeySignal.set('parkItems.passportRide.errors.rating');
      return;
    }

    if (draft.rating !== null && draft.count !== 1) {
      this.errorKeySignal.set('parkItems.passportRide.errors.multipleRating');
      return;
    }

    const request: CreatePassportRideOccurrencesBatchRequest | null = mapParkItemRideDraftToRequest(
      target.parkItemId,
      draft,
      selectedVisit.acceptsLocalTime);
    if (!request) {
      this.errorKeySignal.set('parkItems.passportRide.errors.invalid');
      return;
    }

    const fingerprint: string = JSON.stringify({ visitId: selectedVisit.id, request, rating: draft.rating });
    const pendingSubmission: PendingRideSubmission = this.pendingSubmission?.fingerprint === fingerprint
      ? this.pendingSubmission
      : { fingerprint, idempotencyKey: this.operationIds.create() };
    this.pendingSubmission = pendingSubmission;
    const generation: number = this.configurationGeneration;
    this.savingSignal.set(true);
    this.errorKeySignal.set(null);
    this.outcomeSignal.set(null);
    this.occurrencesApi.addBatch(selectedVisit.id, request, pendingSubmission.idempotencyKey)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: PassportRideOccurrenceMutationResult): void => {
          if (!this.isCurrentConfiguration(generation, target)) {
            return;
          }

          const occurrences: PassportRideOccurrence[] = result.occurrences.filter(
            (occurrence: PassportRideOccurrence): boolean =>
              occurrence.visitId === selectedVisit.id && occurrence.parkItemId === target.parkItemId);
          if (occurrences.length === 0) {
            this.savingSignal.set(false);
            this.errorKeySignal.set('parkItems.passportRide.errors.response');
            return;
          }

          this.pendingSubmission = null;
          this.createdVisitIdSignal.set(selectedVisit.id);
          this.addedCountSignal.set(occurrences.length);
          if (draft.rating === null) {
            this.finishSaving('rideSaved');
            return;
          }

          this.saveAssessments(occurrences, draft.rating, generation, target);
        },
        error: (error: unknown): void => {
          if (!this.isCurrentConfiguration(generation, target)) {
            return;
          }

          console.error('Error adding a ride from the park item page', error);
          this.savingSignal.set(false);
          if (!isAmbiguousMutationError(error)) {
            this.pendingSubmission = null;
          }

          const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
          if (errorCode === 'visit.not-editable') {
            this.removeVisit(selectedVisit.id);
            this.errorKeySignal.set('parkItems.passportRide.errors.visitNoLongerDraft');
            return;
          }

          this.errorKeySignal.set(errorCode === 'ride-occurrence.historical-conflict-confirmation-required'
            ? 'parkItems.passportRide.errors.confirmConflict'
            : isAmbiguousMutationError(error)
              ? 'parkItems.passportRide.errors.network'
              : 'parkItems.passportRide.errors.save');
        }
      });
  }

  dismissOutcome(): void {
    this.outcomeSignal.set(null);
  }

  clearError(): void {
    this.errorKeySignal.set(null);
  }

  private saveAssessments(
    occurrences: PassportRideOccurrence[],
    rating: number,
    generation: number,
    target: ParkItemPassportRideTarget
  ): void {
    const requests: Observable<PassportRideOccurrence>[] = occurrences.map(
      (occurrence: PassportRideOccurrence): Observable<PassportRideOccurrence> =>
        this.occurrencesApi.upsertAssessment(occurrence.id, {
          value: rating,
          privateComment: null,
          expectedVersion: occurrence.version
        }));
    forkJoin(requests).pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (): void => {
        if (this.isCurrentConfiguration(generation, target)) {
          this.finishSaving('rideAndRatingSaved');
        }
      },
      error: (error: unknown): void => {
        if (!this.isCurrentConfiguration(generation, target)) {
          return;
        }

        console.error('A ride was added but its optional assessment could not be saved', error);
        this.finishSaving('rideSavedRatingFailed');
      }
    });
  }

  private finishSaving(outcome: ParkItemPassportRideOutcome): void {
    this.savingSignal.set(false);
    this.errorKeySignal.set(null);
    this.outcomeSignal.set(outcome);
  }

  private removeVisit(visitId: string): void {
    this.visitsSignal.update((visits: PassportVisit[]): PassportVisit[] =>
      visits.filter((visit: PassportVisit): boolean => visit.id !== visitId));
    this.selectedVisitIdSignal.set(null);
    this.evaluationSignal.set(null);
  }

  private filterCompatibleVisits(visits: readonly PassportVisit[], parkId: string): PassportVisit[] {
    return deduplicateVisits(visits.filter((visit: PassportVisit): boolean =>
      visit.status === 'Draft' && visit.parkId === parkId));
  }

  private isCurrentConfiguration(generation: number, target: ParkItemPassportRideTarget): boolean {
    const current: ParkItemPassportRideTarget | null = this.targetSignal();
    return generation === this.configurationGeneration
      && current?.parkId === target.parkId
      && current?.parkItemId === target.parkItemId;
  }

  private isCurrentSelection(
    generation: number,
    visitId: string,
    target: ParkItemPassportRideTarget
  ): boolean {
    return generation === this.selectionGeneration
      && this.selectedVisitIdSignal() === visitId
      && this.targetSignal()?.parkItemId === target.parkItemId;
  }

  private reset(target: ParkItemPassportRideTarget | null): void {
    this.configurationGeneration += 1;
    this.selectionGeneration += 1;
    this.targetSignal.set(target);
    this.visitsSignal.set([]);
    this.selectedVisitIdSignal.set(null);
    this.loadingSignal.set(false);
    this.loadingMoreSignal.set(false);
    this.evaluatingSignal.set(false);
    this.savingSignal.set(false);
    this.errorKeySignal.set(null);
    this.nextCursorSignal.set(null);
    this.evaluationSignal.set(null);
    this.outcomeSignal.set(null);
    this.createdVisitIdSignal.set(null);
    this.addedCountSignal.set(0);
    this.pendingSubmission = null;
  }
}

function normalizeTarget(target: ParkItemPassportRideTarget): ParkItemPassportRideTarget | null {
  const normalized: ParkItemPassportRideTarget = {
    parkItemId: target.parkItemId?.trim() ?? '',
    parkItemName: target.parkItemName?.trim() ?? '',
    parkId: target.parkId?.trim() ?? '',
    parkName: target.parkName?.trim() ?? '',
    language: target.language?.trim() || 'en'
  };
  return normalized.parkItemId && normalized.parkId ? normalized : null;
}

function normalizeCursor(cursor: string | null | undefined): string | null {
  return cursor?.trim() || null;
}

function deduplicateVisits(visits: readonly PassportVisit[]): PassportVisit[] {
  const byId = new Map<string, PassportVisit>();
  for (const visit of visits) {
    if (visit.id && !byId.has(visit.id)) {
      byId.set(visit.id, visit);
    }
  }

  return Array.from(byId.values());
}

function isAmbiguousMutationError(error: unknown): boolean {
  return error instanceof HttpErrorResponse
    && (error.status === 0 || error.status === 408 || error.status >= 500);
}
