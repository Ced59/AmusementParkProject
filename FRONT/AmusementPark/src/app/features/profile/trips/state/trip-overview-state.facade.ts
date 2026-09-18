import { computed, DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  concatMap,
  defer,
  EMPTY,
  finalize,
  forkJoin,
  from,
  last,
  map,
  Observable,
  of,
  switchMap,
  tap,
  throwError
} from 'rxjs';

import {
  AddTripParkCandidateRequest,
  ChangeTripParkCandidateStateRequest,
  MoveTripParkCandidateRequest,
  PutTripDayPlanRequest,
  SetTripPlanDatesRequest,
  TripDayPlan,
  TripParkCandidate,
  TripParkCandidatePlacement,
  TripParkCandidateState,
  TripPlan,
  TripProgram
} from '@app/models/trips/trip.models';
import { UserCollectionEntry } from '@app/models/watchlists/user-collection-entry.model';
import { areTripDateInputsValid, buildConfirmedTripDates, enumerateTripDates } from './trip-date-proposal.helpers';
import {
  TRIP_COLLECTIONS_DATA_PORT,
  TRIP_OPERATION_ID_PORT,
  TRIP_PLANS_DATA_PORT,
  TRIP_PROGRAM_DATA_PORT,
  TripCollectionsDataPort,
  TripOperationIdPort,
  TripPlansDataPort,
  TripProgramDataPort
} from './trip-state-data.ports';

export type TripOverviewStatus = 'idle' | 'loading' | 'ready' | 'error';
export type TripOverviewActionError = 'conflict' | 'failed' | null;

@Injectable()
export class TripOverviewStateFacade {
  private readonly tripIdSignal = signal<string | null>(null);
  private readonly tripSignal = signal<TripPlan | null>(null);
  private readonly programSignal = signal<TripProgram>({ candidates: [], days: [] });
  private readonly collectionsSignal = signal<UserCollectionEntry[]>([]);
  private readonly statusSignal = signal<TripOverviewStatus>('idle');
  private readonly busySignal = signal<boolean>(false);
  private readonly actionErrorSignal = signal<TripOverviewActionError>(null);
  private readonly wishlistUnavailableSignal = signal<boolean>(false);
  private readonly recoveryRevisionSignal = signal<number>(0);
  private readonly dateDraftRevisionSignal = signal<number>(0);
  private readonly clearedDaySignal = signal<{ localDate: string; revision: number } | null>(null);
  private readonly wishlistImportOperations = new Map<string, { fingerprint: string; operationId: string }>();

  readonly trip: Signal<TripPlan | null> = this.tripSignal.asReadonly();
  readonly program: Signal<TripProgram> = this.programSignal.asReadonly();
  readonly status: Signal<TripOverviewStatus> = this.statusSignal.asReadonly();
  readonly busy: Signal<boolean> = this.busySignal.asReadonly();
  readonly actionError: Signal<TripOverviewActionError> = this.actionErrorSignal.asReadonly();
  readonly wishlistUnavailable: Signal<boolean> = this.wishlistUnavailableSignal.asReadonly();
  readonly recoveryRevision: Signal<number> = this.recoveryRevisionSignal.asReadonly();
  readonly dateDraftRevision: Signal<number> = this.dateDraftRevisionSignal.asReadonly();
  readonly clearedDay: Signal<{ localDate: string; revision: number } | null> = this.clearedDaySignal.asReadonly();
  readonly tripDates: Signal<string[]> = computed((): string[] => {
    const trip: TripPlan | null = this.tripSignal();
    return trip ? enumerateTripDates(trip.dateProposal) : [];
  });
  readonly wishlistParks: Signal<UserCollectionEntry[]> = computed((): UserCollectionEntry[] => {
    const existingParkIds: Set<string> = new Set(
      this.programSignal().candidates.map((candidate: TripParkCandidate): string => candidate.parkId)
    );
    const eligibleEntries: UserCollectionEntry[] = this.collectionsSignal().filter((entry: UserCollectionEntry): boolean =>
      entry.targetType === 'Park'
      && (entry.kind === 'WantToVisit' || entry.kind === 'Planned')
      && entry.targetStatus !== 'PermanentlyClosed'
      && entry.targetName !== null
      && !existingParkIds.has(entry.targetId));
    return this.deduplicateWishlistEntries(eligibleEntries);
  });

  constructor(
    @Inject(TRIP_PLANS_DATA_PORT) private readonly plans: TripPlansDataPort,
    @Inject(TRIP_PROGRAM_DATA_PORT) private readonly programData: TripProgramDataPort,
    @Inject(TRIP_COLLECTIONS_DATA_PORT) private readonly collections: TripCollectionsDataPort,
    @Inject(TRIP_OPERATION_ID_PORT) private readonly operationIds: TripOperationIdPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(tripId: string): void {
    if (!tripId || this.statusSignal() === 'loading') {
      return;
    }

    this.tripIdSignal.set(tripId);
    this.statusSignal.set('loading');
    this.collectionsSignal.set([]);
    this.wishlistUnavailableSignal.set(false);
    forkJoin({
      trip: this.plans.getMine(tripId),
      program: this.programData.get(tripId)
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: { trip: TripPlan; program: TripProgram }): void => {
          this.tripSignal.set(result.trip);
          this.programSignal.set(result.program);
          this.wishlistImportOperations.clear();
          this.statusSignal.set('ready');
        },
        error: (): void => this.statusSignal.set('error')
      });
    this.loadWishlist();
  }

  setDates(startDate: string, endDate: string, destinationTimeZoneId: string): void {
    const trip: TripPlan | null = this.tripSignal();
    const tripId: string | null = this.tripIdSignal();
    const normalizedTimeZoneId: string = destinationTimeZoneId.trim();
    if (!trip
      || !tripId
      || this.busySignal()
      || !areTripDateInputsValid(startDate, endDate)) {
      return;
    }
    const dateProposal = buildConfirmedTripDates(startDate, endDate);
    if (dateProposal.kind !== 'None' && !normalizedTimeZoneId) {
      return;
    }

    const request: SetTripPlanDatesRequest = {
      expectedVersion: trip.version,
      dateProposal,
      destinationTimeZoneId: dateProposal.kind === 'None' ? null : normalizedTimeZoneId
    };
    this.runAction(this.plans.setDates(tripId, request).pipe(
      tap((updated: TripPlan): void => {
        this.tripSignal.set(updated);
        this.dateDraftRevisionSignal.update((revision: number): number => revision + 1);
      }),
      switchMap((): Observable<TripProgram> => this.programData.get(tripId)),
      tap((program: TripProgram): void => this.programSignal.set(program))
    ));
  }

  importWishlist(entries: UserCollectionEntry[]): void {
    const tripId: string | null = this.tripIdSignal();
    const existingParkIds: Set<string> = new Set(
      this.programSignal().candidates.map((candidate: TripParkCandidate): string => candidate.parkId)
    );
    const uniqueEntries: UserCollectionEntry[] = this.deduplicateWishlistEntries(entries)
      .filter((entry: UserCollectionEntry): boolean => !existingParkIds.has(entry.targetId));
    if (!tripId || uniqueEntries.length === 0 || this.busySignal()) {
      return;
    }

    const operation: Observable<TripPlan> = from(uniqueEntries).pipe(
      concatMap((entry: UserCollectionEntry): Observable<TripPlan> => defer((): Observable<TripPlan> => {
        const trip: TripPlan | null = this.tripSignal();
        if (!trip) {
          throw new Error('Trip is unavailable.');
        }
        const request: AddTripParkCandidateRequest = {
          expectedPlanVersion: trip.version,
          parkId: entry.targetId,
          candidateDates: [],
          source: 'Wishlist',
          collectiveNote: null
        };
        const operationId: string = this.resolveWishlistImportOperationId(tripId, entry.targetId, request);
        return this.programData.addPark(tripId, request, operationId).pipe(
          tap((candidate: TripParkCandidate): void => {
            this.wishlistImportOperations.delete(entry.targetId);
            this.reconcileImportedCandidate(candidate);
          }),
          catchError((error: { status?: number }): Observable<TripParkCandidate> => {
            if (!this.requiresRecoveryReload(error)) {
              this.wishlistImportOperations.delete(entry.targetId);
            }
            return throwError(() => error);
          }),
          switchMap((): Observable<TripPlan> => this.plans.getMine(tripId)),
          tap((updatedTrip: TripPlan): void => this.tripSignal.set(updatedTrip))
        );
      }))
    );

    this.runAction(operation.pipe(
      last(),
      switchMap((): Observable<TripProgram> => this.programData.get(tripId)),
      tap((program: TripProgram): void => this.programSignal.set(program)),
      map((): void => undefined)
    ));
  }

  changeCandidateState(candidate: TripParkCandidate, state: TripParkCandidateState): void {
    const trip: TripPlan | null = this.tripSignal();
    const tripId: string | null = this.tripIdSignal();
    if (!trip || !tripId || this.busySignal() || candidate.state === state) {
      return;
    }

    const request: ChangeTripParkCandidateStateRequest = {
      expectedPlanVersion: trip.version,
      expectedCandidateVersion: candidate.version,
      state
    };
    this.runAction(this.programData.changeParkState(tripId, candidate.candidateId, request).pipe(
      switchMap((): Observable<{ trip: TripPlan; program: TripProgram }> => this.reloadProgram(tripId)),
      tap((result: { trip: TripPlan; program: TripProgram }): void => this.applyProgram(result))
    ));
  }

  moveCandidate(candidateId: string, targetIndex: number): void {
    const trip: TripPlan | null = this.tripSignal();
    const tripId: string | null = this.tripIdSignal();
    const candidates: TripParkCandidate[] = this.programSignal().candidates;
    const currentIndex: number = candidates.findIndex(
      (candidate: TripParkCandidate): boolean => candidate.candidateId === candidateId
    );
    if (!trip || !tripId || this.busySignal() || currentIndex < 0 || currentIndex === targetIndex) {
      return;
    }

    const reordered: TripParkCandidate[] = candidates.filter(
      (candidate: TripParkCandidate): boolean => candidate.candidateId !== candidateId
    );
    const boundedIndex: number = Math.max(0, Math.min(targetIndex, reordered.length));
    const placement: TripParkCandidatePlacement = boundedIndex === 0 ? 'First' : 'After';
    const anchorCandidateId: string | null = boundedIndex === 0
      ? null
      : reordered[boundedIndex - 1].candidateId;
    const request: MoveTripParkCandidateRequest = {
      expectedPlanVersion: trip.version,
      anchorCandidateId,
      placement
    };

    this.runAction(this.programData.movePark(tripId, candidateId, request).pipe(
      switchMap((program: TripProgram): Observable<{ trip: TripPlan; program: TripProgram }> =>
        this.plans.getMine(tripId).pipe(map((updatedTrip: TripPlan) => ({ trip: updatedTrip, program })))),
      tap((result: { trip: TripPlan; program: TripProgram }): void => this.applyProgram(result))
    ));
  }

  saveDay(localDate: string, parkCandidateId: string, desiredArrivalTime: string, groupNote: string): void {
    const trip: TripPlan | null = this.tripSignal();
    const tripId: string | null = this.tripIdSignal();
    if (!trip || !tripId || !parkCandidateId || this.busySignal()) {
      return;
    }

    const existing: TripDayPlan | undefined = this.programSignal().days.find(
      (day: TripDayPlan): boolean => day.localDate === localDate
    );
    const request: PutTripDayPlanRequest = {
      expectedPlanVersion: trip.version,
      expectedDayVersion: existing?.version ?? null,
      parkCandidateId,
      desiredArrivalTime: desiredArrivalTime || null,
      groupNote: groupNote.trim() || null,
      blocks: existing?.blocks ?? []
    };
    this.runAction(this.programData.putDay(tripId, localDate, request).pipe(
      switchMap((): Observable<{ trip: TripPlan; program: TripProgram }> => this.reloadProgram(tripId)),
      tap((result: { trip: TripPlan; program: TripProgram }): void => this.applyProgram(result))
    ));
  }

  clearDay(localDate: string): void {
    const trip: TripPlan | null = this.tripSignal();
    const tripId: string | null = this.tripIdSignal();
    const existing: TripDayPlan | undefined = this.programSignal().days.find(
      (day: TripDayPlan): boolean => day.localDate === localDate
    );
    if (!trip || !tripId || !existing || this.busySignal()) {
      return;
    }

    this.runAction(this.programData.deleteDay(tripId, localDate, trip.version, existing.version).pipe(
      switchMap((): Observable<{ trip: TripPlan; program: TripProgram }> => this.reloadProgram(tripId)),
      tap((result: { trip: TripPlan; program: TripProgram }): void => {
        this.applyProgram(result);
        this.clearedDaySignal.update((current: { localDate: string; revision: number } | null) => ({
          localDate,
          revision: (current?.revision ?? 0) + 1
        }));
      })
    ));
  }

  imageIdForPark(parkId: string): string | null {
    return this.collectionsSignal().find(
      (entry: UserCollectionEntry): boolean => entry.targetType === 'Park' && entry.targetId === parkId
    )?.mainImageId ?? null;
  }

  private runAction(operation: Observable<unknown>): void {
    this.busySignal.set(true);
    this.actionErrorSignal.set(null);
    operation.pipe(
      takeUntilDestroyed(this.destroyRef),
      catchError((error: { status?: number }): Observable<unknown> => {
        this.actionErrorSignal.set(error?.status === 409 ? 'conflict' : 'failed');
        return this.requiresRecoveryReload(error) ? this.reloadAfterFailure() : EMPTY;
      }),
      finalize((): void => this.busySignal.set(false))
    ).subscribe();
  }

  private requiresRecoveryReload(error: { status?: number }): boolean {
    const status: number | undefined = error?.status;
    return status === undefined || status === 0 || status === 408 || status === 409 || status >= 500;
  }

  private reloadAfterFailure(): Observable<void> {
    const tripId: string | null = this.tripIdSignal();
    if (!tripId) {
      return of(undefined);
    }
    return this.reloadProgram(tripId).pipe(
      tap((result: { trip: TripPlan; program: TripProgram }): void => {
        this.applyProgram(result);
        this.wishlistImportOperations.clear();
        this.recoveryRevisionSignal.update((revision: number): number => revision + 1);
        this.dateDraftRevisionSignal.update((revision: number): number => revision + 1);
      }),
      map((): void => undefined),
      catchError((): Observable<void> => of(undefined))
    );
  }

  private reloadProgram(tripId: string): Observable<{ trip: TripPlan; program: TripProgram }> {
    return forkJoin({
      trip: this.plans.getMine(tripId),
      program: this.programData.get(tripId)
    });
  }

  private applyProgram(result: { trip: TripPlan; program: TripProgram }): void {
    this.tripSignal.set(result.trip);
    this.programSignal.set(result.program);
  }

  private loadWishlist(): void {
    this.collections.listMine('Park')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (collections: UserCollectionEntry[]): void => this.collectionsSignal.set(collections),
        error: (): void => this.wishlistUnavailableSignal.set(true)
      });
  }

  private resolveWishlistImportOperationId(
    tripId: string,
    parkId: string,
    request: AddTripParkCandidateRequest
  ): string {
    const fingerprint: string = JSON.stringify({ tripId, request });
    const pending: { fingerprint: string; operationId: string } | undefined =
      this.wishlistImportOperations.get(parkId);
    if (pending?.fingerprint === fingerprint) {
      return pending.operationId;
    }

    const operationId: string = this.operationIds.create();
    this.wishlistImportOperations.set(parkId, { fingerprint, operationId });
    return operationId;
  }

  private reconcileImportedCandidate(candidate: TripParkCandidate): void {
    this.programSignal.update((program: TripProgram): TripProgram => {
      const existingIndex: number = program.candidates.findIndex(
        (current: TripParkCandidate): boolean => current.candidateId === candidate.candidateId
          || current.parkId === candidate.parkId
      );
      if (existingIndex < 0) {
        return { ...program, candidates: [...program.candidates, candidate] };
      }

      const candidates: TripParkCandidate[] = [...program.candidates];
      candidates[existingIndex] = candidate;
      return { ...program, candidates };
    });
  }

  private deduplicateWishlistEntries(entries: UserCollectionEntry[]): UserCollectionEntry[] {
    const entriesByParkId: Map<string, UserCollectionEntry> = new Map<string, UserCollectionEntry>();
    for (const entry of entries) {
      const current: UserCollectionEntry | undefined = entriesByParkId.get(entry.targetId);
      if (!current || (entry.kind === 'Planned' && current.kind !== 'Planned')) {
        entriesByParkId.set(entry.targetId, entry);
      }
    }
    return Array.from(entriesByParkId.values());
  }
}
