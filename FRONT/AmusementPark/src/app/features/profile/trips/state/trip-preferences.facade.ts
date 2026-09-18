import { HttpErrorResponse } from '@angular/common/http';
import { computed, DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { concatMap, finalize, from, last, Observable } from 'rxjs';

import {
  BulkSetTripItemPreferencesRequest,
  BulkTripItemPreferenceRequest,
  SetTripItemPreferenceRequest,
  TRIP_ITEM_PREFERENCE_MAX_BATCH_SIZE,
  TripItemPreference,
  TripItemPreferenceLevel,
  TripItemPreferenceReason,
  TripPreferenceBoard
} from '@app/models/trips/trip.models';
import {
  TRIP_PREFERENCES_DATA_PORT,
  TripPreferencesDataPort
} from './trip-preferences-data.port';

interface TripPreferenceDraft {
  level: TripItemPreferenceLevel;
  reason: TripItemPreferenceReason | null;
}

export interface TripPreferenceParkOption {
  parkId: string;
  parkName: string;
}

@Injectable()
export class TripPreferencesFacade {
  private readonly boardSignal = signal<TripPreferenceBoard | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly feedbackKeySignal = signal<string | null>(null);
  private readonly draftsSignal = signal<Record<string, TripPreferenceDraft>>({});
  private readonly searchSignal = signal<string>('');
  private readonly parkFilterSignal = signal<string>('');
  private readonly levelFilterSignal = signal<TripItemPreferenceLevel | ''>('');
  private tripPlanId: string = '';

  readonly board: Signal<TripPreferenceBoard | null> = this.boardSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly feedbackKey: Signal<string | null> = this.feedbackKeySignal.asReadonly();
  readonly search: Signal<string> = this.searchSignal.asReadonly();
  readonly parkFilter: Signal<string> = this.parkFilterSignal.asReadonly();
  readonly levelFilter: Signal<TripItemPreferenceLevel | ''> = this.levelFilterSignal.asReadonly();
  readonly pendingCount: Signal<number> = computed((): number => Object.keys(this.draftsSignal()).length);
  readonly parkOptions: Signal<TripPreferenceParkOption[]> = computed((): TripPreferenceParkOption[] => {
    const unique: Map<string, string> = new Map<string, string>();
    for (const item of this.boardSignal()?.items ?? []) {
      unique.set(item.parkId, item.parkName);
    }
    return Array.from(unique, ([parkId, parkName]: [string, string]): TripPreferenceParkOption => ({
      parkId,
      parkName
    }));
  });
  readonly visibleItems: Signal<TripItemPreference[]> = computed((): TripItemPreference[] => {
    const query: string = this.searchSignal().trim().toLocaleLowerCase();
    const parkId: string = this.parkFilterSignal();
    const level: TripItemPreferenceLevel | '' = this.levelFilterSignal();
    return (this.boardSignal()?.items ?? [])
      .map((item: TripItemPreference): TripItemPreference => this.withDraft(item))
      .filter((item: TripItemPreference): boolean =>
        (!parkId || item.parkId === parkId)
        && (!level || item.level === level)
        && (!query || item.parkItemName.toLocaleLowerCase().includes(query)
          || item.parkName.toLocaleLowerCase().includes(query)));
  });

  constructor(
    @Inject(TRIP_PREFERENCES_DATA_PORT) private readonly data: TripPreferencesDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(tripPlanId: string): void {
    const normalizedId: string = tripPlanId.trim();
    if (!normalizedId || this.loadingSignal()) {
      return;
    }

    this.tripPlanId = normalizedId;
    this.loadingSignal.set(true);
    this.feedbackKeySignal.set(null);
    this.data.getMine(normalizedId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.loadingSignal.set(false))
    ).subscribe({
      next: (board: TripPreferenceBoard): void => {
        this.boardSignal.set(board);
        this.draftsSignal.set({});
      },
      error: (): void => this.feedbackKeySignal.set('trips.preferences.feedback.loadError')
    });
  }

  setSearch(value: string): void {
    this.searchSignal.set(value);
  }

  setParkFilter(value: string): void {
    this.parkFilterSignal.set(value);
  }

  setLevelFilter(value: TripItemPreferenceLevel | ''): void {
    this.levelFilterSignal.set(value);
  }

  setLevel(parkItemId: string, level: TripItemPreferenceLevel): void {
    if (!this.boardSignal()?.canVote || this.savingSignal()) {
      return;
    }

    const item: TripItemPreference | undefined = this.findItem(parkItemId);
    if (!item) {
      return;
    }

    const current: TripPreferenceDraft = this.draftsSignal()[parkItemId]
      ?? { level: item.level, reason: item.reason };
    this.updateDraft(parkItemId, {
      level,
      reason: level === 'Unknown' ? null : current.reason
    });
  }

  setReason(parkItemId: string, reason: TripItemPreferenceReason | null): void {
    if (!this.boardSignal()?.canVote || this.savingSignal()) {
      return;
    }

    const item: TripItemPreference | undefined = this.findItem(parkItemId);
    if (!item) {
      return;
    }

    const current: TripPreferenceDraft = this.draftsSignal()[parkItemId]
      ?? { level: item.level, reason: item.reason };
    this.updateDraft(parkItemId, {
      level: current.level,
      reason: current.level === 'Unknown' ? null : reason
    });
  }

  discardChanges(): void {
    if (!this.savingSignal()) {
      this.draftsSignal.set({});
      this.feedbackKeySignal.set(null);
    }
  }

  save(): void {
    const board: TripPreferenceBoard | null = this.boardSignal();
    const drafts: Record<string, TripPreferenceDraft> = this.draftsSignal();
    const entries: Array<[string, TripPreferenceDraft]> = Object.entries(drafts);
    if (!board?.canVote || this.savingSignal() || entries.length === 0) {
      return;
    }

    const requests: BulkTripItemPreferenceRequest[] = entries.map(
      ([parkItemId, draft]: [string, TripPreferenceDraft]): BulkTripItemPreferenceRequest => {
        const item: TripItemPreference | undefined = board.items.find(
          (candidate: TripItemPreference): boolean => candidate.parkItemId === parkItemId
        );
        return {
          parkItemId,
          expectedPreferenceVersion: item?.version ?? null,
          level: draft.level,
          reason: draft.reason
        };
      }
    );
    let operation: Observable<TripPreferenceBoard>;
    if (requests.length === 1) {
      const request: SetTripItemPreferenceRequest = {
        expectedPlanVersion: board.planVersion,
        expectedPreferenceVersion: requests[0].expectedPreferenceVersion,
        level: requests[0].level,
        reason: requests[0].reason
      };
      operation = this.data.set(this.tripPlanId, requests[0].parkItemId, request);
    } else {
      const batches: BulkTripItemPreferenceRequest[][] = chunkPreferences(requests);
      operation = from(batches).pipe(
        concatMap((preferences: BulkTripItemPreferenceRequest[]): Observable<TripPreferenceBoard> => {
          const request: BulkSetTripItemPreferencesRequest = {
            expectedPlanVersion: board.planVersion,
            preferences
          };
          return this.data.setBatch(this.tripPlanId, request);
        }),
        last()
      );
    }

    this.savingSignal.set(true);
    this.feedbackKeySignal.set(null);
    operation.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.savingSignal.set(false))
    ).subscribe({
      next: (updated: TripPreferenceBoard): void => {
        this.boardSignal.set(updated);
        this.draftsSignal.set({});
        this.feedbackKeySignal.set('trips.preferences.feedback.saved');
      },
      error: (error: HttpErrorResponse): void => {
        this.feedbackKeySignal.set(error.status === 409
          ? 'trips.preferences.feedback.conflict'
          : 'trips.preferences.feedback.saveError');
      }
    });
  }

  private withDraft(item: TripItemPreference): TripItemPreference {
    const draft: TripPreferenceDraft | undefined = this.draftsSignal()[item.parkItemId];
    return draft ? { ...item, level: draft.level, reason: draft.reason } : item;
  }

  private findItem(parkItemId: string): TripItemPreference | undefined {
    return this.boardSignal()?.items.find(
      (item: TripItemPreference): boolean => item.parkItemId === parkItemId
    );
  }

  private updateDraft(parkItemId: string, draft: TripPreferenceDraft): void {
    const stored: TripItemPreference | undefined = this.findItem(parkItemId);
    this.draftsSignal.update((current: Record<string, TripPreferenceDraft>): Record<string, TripPreferenceDraft> => {
      const next: Record<string, TripPreferenceDraft> = { ...current };
      if (stored && stored.level === draft.level && stored.reason === draft.reason) {
        delete next[parkItemId];
      } else {
        next[parkItemId] = draft;
      }
      return next;
    });
    this.feedbackKeySignal.set(null);
  }
}

function chunkPreferences(
  preferences: BulkTripItemPreferenceRequest[]
): BulkTripItemPreferenceRequest[][] {
  const chunks: BulkTripItemPreferenceRequest[][] = [];
  for (
    let index: number = 0;
    index < preferences.length;
    index += TRIP_ITEM_PREFERENCE_MAX_BATCH_SIZE
  ) {
    chunks.push(preferences.slice(index, index + TRIP_ITEM_PREFERENCE_MAX_BATCH_SIZE));
  }
  return chunks;
}
