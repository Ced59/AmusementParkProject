import { DestroyRef, Inject, Injectable, Signal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { take } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';
import { PagedResult } from '@shared/models/contracts';
import { PassportAttractionSelectionDraft, PassportVisitEditorAttraction } from '../../models/passport-visit-editor.models';
import { mapParkItemToVisitEditorAttraction } from '../../mappers/passport-visit-editor.mapper';
import { PassportOperationIdService } from '@data-access/passport/passport-operation-id.service';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import {
  PASSPORT_ANONYMOUS_DRAFT_MAX_RIDE_COUNT,
  PassportAnonymousDraft,
  PassportAnonymousRideDraft
} from '../models/passport-anonymous-draft.models';
import {
  PASSPORT_ANONYMOUS_DRAFT_STORE_PORT,
  PassportAnonymousDraftStorePort
} from './passport-anonymous-draft-store.ports';
import {
  PASSPORT_ANONYMOUS_DRAFT_ATTRACTIONS_PORT,
  PassportAnonymousDraftAttractionsPort
} from './passport-anonymous-draft-editor-data.ports';

@Injectable()
export class PassportAnonymousDraftEditorStateFacade {
  private static readonly AttractionPageSize: number = 20;

  private readonly draftSignal = signal<PassportAnonymousDraft | null>(null);
  private readonly attractionsSignal = signal<PassportVisitEditorAttraction[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly attractionsLoadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly attractionErrorKeySignal = signal<string | null>(null);
  private readonly hasMoreAttractionsSignal = signal<boolean>(false);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private attractionPage: number = 1;
  private attractionSearch: string = '';
  private attractionLoadGeneration: number = 0;

  readonly draft: Signal<PassportAnonymousDraft | null> = this.draftSignal.asReadonly();
  readonly attractions: Signal<PassportVisitEditorAttraction[]> = this.attractionsSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly attractionsLoading: Signal<boolean> = this.attractionsLoadingSignal.asReadonly();
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  readonly attractionErrorKey: Signal<string | null> = this.attractionErrorKeySignal.asReadonly();
  readonly hasMoreAttractions: Signal<boolean> = this.hasMoreAttractionsSignal.asReadonly();
  readonly isImportLocked: Signal<boolean> = computed((): boolean =>
    !!this.draftSignal()?.pendingImport);
  readonly acceptsLocalTime: Signal<boolean> = computed((): boolean => {
    const draft: PassportAnonymousDraft | null = this.draftSignal();
    return draft?.visit.date.precision === 'Day' && !!draft.visit.timeZoneId?.trim();
  });

  constructor(
    @Inject(PASSPORT_ANONYMOUS_DRAFT_STORE_PORT)
    private readonly store: PassportAnonymousDraftStorePort,
    @Inject(PASSPORT_ANONYMOUS_DRAFT_ATTRACTIONS_PORT)
    private readonly attractionsApi: PassportAnonymousDraftAttractionsPort,
    private readonly operationIds: PassportOperationIdService
  ) {
  }

  async load(draftId: string): Promise<void> {
    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);
    try {
      const draft: PassportAnonymousDraft | null = await this.store.get(draftId);
      this.draftSignal.set(draft);
      if (!draft) {
        this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.notFound');
        return;
      }

      if (!draft.pendingImport) {
        this.loadAttractions(1, false);
      }
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.load');
    } finally {
      this.loadingSignal.set(false);
    }
  }

  searchAttractions(search: string): void {
    if (this.isImportLocked()) {
      return;
    }

    this.attractionSearch = search.trim();
    this.loadAttractions(1, false);
  }

  loadMoreAttractions(): void {
    if (this.isImportLocked()
      || !this.hasMoreAttractionsSignal()
      || this.attractionsLoadingSignal()) {
      return;
    }

    this.loadAttractions(this.attractionPage + 1, true);
  }

  async addRide(selection: PassportAttractionSelectionDraft): Promise<boolean> {
    const draft: PassportAnonymousDraft | null = this.draftSignal();
    if (!draft || draft.pendingImport || this.savingSignal()) {
      if (draft?.pendingImport) {
        this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.importLocked');
      }
      return false;
    }

    const parkItemId: string = selection.parkItemId.trim();
    const attractionName: string = selection.attractionName.trim();
    const count: number = Math.trunc(selection.count);
    if (!parkItemId
      || !attractionName
      || count < 1
      || count > 100
      || this.totalRideCount() + count > PASSPORT_ANONYMOUS_DRAFT_MAX_RIDE_COUNT) {
      this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.invalidRide');
      return false;
    }

    const localTime: string | null = this.acceptsLocalTime()
      ? selection.localTime.trim() || null
      : null;
    if (localTime && !/^([01]\d|2[0-3]):[0-5]\d$/.test(localTime)) {
      this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.invalidRide');
      return false;
    }

    const ride: PassportAnonymousRideDraft = {
      id: this.operationIds.create(),
      parkItemId,
      attractionName,
      moment: {
        localTime,
        isApproximate: localTime !== null && selection.isApproximate
      },
      status: selection.status,
      privateNote: selection.privateNote.trim() || null,
      confirmHistoricalConflict: selection.confirmHistoricalConflict,
      count
    };
    return await this.persist({
      ...draft,
      rides: [...draft.rides, ride],
      updatedAtUtc: new Date().toISOString()
    });
  }

  removeRide(rideId: string): void {
    const draft: PassportAnonymousDraft | null = this.draftSignal();
    if (!draft || draft.pendingImport || this.savingSignal()) {
      if (draft?.pendingImport) {
        this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.importLocked');
      }
      return;
    }

    void this.persist({
      ...draft,
      rides: draft.rides.filter((ride: PassportAnonymousRideDraft): boolean =>
        ride.id !== rideId),
      updatedAtUtc: new Date().toISOString()
    });
  }

  moveRide(rideId: string, direction: -1 | 1): void {
    const draft: PassportAnonymousDraft | null = this.draftSignal();
    if (!draft || draft.pendingImport || this.savingSignal()) {
      if (draft?.pendingImport) {
        this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.importLocked');
      }
      return;
    }

    const currentIndex: number = draft.rides.findIndex(
      (ride: PassportAnonymousRideDraft): boolean => ride.id === rideId
    );
    const targetIndex: number = currentIndex + direction;
    if (currentIndex < 0 || targetIndex < 0 || targetIndex >= draft.rides.length) {
      return;
    }

    const reordered: PassportAnonymousRideDraft[] = [...draft.rides];
    const [moved]: PassportAnonymousRideDraft[] = reordered.splice(currentIndex, 1);
    reordered.splice(targetIndex, 0, moved);
    void this.persist({
      ...draft,
      rides: reordered,
      updatedAtUtc: new Date().toISOString()
    });
  }

  async deleteDraft(): Promise<boolean> {
    const draft: PassportAnonymousDraft | null = this.draftSignal();
    const draftId: string = draft?.id ?? '';
    if (!draftId || draft?.pendingImport || this.savingSignal()) {
      if (draft?.pendingImport) {
        this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.importLocked');
      }
      return false;
    }

    this.savingSignal.set(true);
    this.errorKeySignal.set(null);
    try {
      await this.store.delete(draftId);
      this.draftSignal.set(null);
      return true;
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.delete');
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }

  totalRideCount(): number {
    return this.draftSignal()?.rides.reduce(
      (total: number, ride: PassportAnonymousRideDraft): number => total + ride.count,
      0
    ) ?? 0;
  }

  private loadAttractions(page: number, append: boolean): void {
    const parkId: string = this.draftSignal()?.visit.parkId ?? '';
    if (!parkId) {
      return;
    }

    const generation: number = ++this.attractionLoadGeneration;
    this.attractionsLoadingSignal.set(true);
    this.attractionErrorKeySignal.set(null);
    this.attractionsApi.getParkItemsByParkIdPage(
      parkId,
      page,
      PassportAnonymousDraftEditorStateFacade.AttractionPageSize,
      {
        closedFilter: 'all',
        category: 'Attraction',
        search: this.attractionSearch || null
      },
      { ...anonymousHttpOptions(), closedFilter: 'all' }
    ).pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: PagedResult<ParkItem>): void => {
        if (generation !== this.attractionLoadGeneration) {
          return;
        }

        const mapped: PassportVisitEditorAttraction[] = result.items
          .map((item: ParkItem): PassportVisitEditorAttraction | null =>
            mapParkItemToVisitEditorAttraction(item))
          .filter((item: PassportVisitEditorAttraction | null): item is PassportVisitEditorAttraction =>
            item !== null);
        this.attractionsSignal.set(append
          ? [...this.attractionsSignal(), ...mapped]
          : mapped);
        this.attractionPage = page;
        this.hasMoreAttractionsSignal.set(page < result.pagination.totalPages);
        this.attractionsLoadingSignal.set(false);
      },
      error: (): void => {
        if (generation !== this.attractionLoadGeneration) {
          return;
        }

        this.attractionsLoadingSignal.set(false);
        this.attractionErrorKeySignal.set('passport.anonymousDrafts.editor.errors.attractions');
      }
    });
  }

  private async persist(updatedDraft: PassportAnonymousDraft): Promise<boolean> {
    const expectedDraft: PassportAnonymousDraft | null = this.draftSignal();
    if (!expectedDraft || expectedDraft.id !== updatedDraft.id) {
      this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.save');
      return false;
    }

    this.savingSignal.set(true);
    this.errorKeySignal.set(null);
    try {
      const persisted: boolean = await this.store.compareAndSet(expectedDraft, updatedDraft);
      if (!persisted) {
        this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.save');
        return false;
      }

      this.draftSignal.set(updatedDraft);
      return true;
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.editor.errors.save');
      return false;
    } finally {
      this.savingSignal.set(false);
    }
  }
}
