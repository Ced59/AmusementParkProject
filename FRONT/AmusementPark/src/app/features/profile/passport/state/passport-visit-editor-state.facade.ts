import { HttpErrorResponse } from '@angular/common/http';
import { computed, DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { catchError, forkJoin, Observable, of, take, throwError } from 'rxjs';

import {
  CreatePassportRideOccurrenceBatchItem,
  CreatePassportRideOccurrencesBatchRequest,
  PassportRideOccurrence,
  PassportRideOccurrenceMutationResult,
  PassportRideOccurrencePage,
  PassportRideOccurrencePlacement,
  ReorderPassportRideOccurrenceRequest
} from '@app/models/passport/passport-ride-occurrence.models';
import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { ParkItem } from '@app/models/parks/park-item';
import { Park } from '@app/models/parks/park';
import { ParkZone } from '@app/models/parks/park-zone';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { extractApiProblemDetails } from '@shared/utils/security/error-display.helpers';
import {
  createAttractionSelection,
  mapAttractionSelectionToRequest,
  mapOccurrenceToEditDraft,
  mapOccurrenceEditToRequest,
  mapParkItemToVisitEditorAttraction,
  mapParkZoneToVisitEditorZone,
  normalizeCount
} from '../mappers/passport-visit-editor.mapper';
import {
  PassportAttractionSelectionDraft,
  PassportAttractionSelectionPatch,
  PassportOccurrenceEditDraft,
  PassportRideOccurrenceRow,
  PassportVisitEditorAttraction,
  PassportVisitEditorZone
} from '../models/passport-visit-editor.models';
import {
  PASSPORT_VISIT_EDITOR_ATTRACTIONS_PORT,
  PASSPORT_VISIT_EDITOR_OCCURRENCES_PORT,
  PASSPORT_VISIT_EDITOR_OPERATION_ID_PORT,
  PASSPORT_VISIT_EDITOR_PARKS_PORT,
  PASSPORT_VISIT_EDITOR_VISITS_PORT,
  PASSPORT_VISIT_EDITOR_ZONES_PORT,
  PassportVisitEditorAttractionsPort,
  PassportVisitEditorOccurrencesPort,
  PassportVisitEditorOperationIdPort,
  PassportVisitEditorParksPort,
  PassportVisitEditorVisitsPort,
  PassportVisitEditorZonesPort
} from './passport-visit-editor-data.ports';

interface InitialVisitEditorData {
  park: Park | null;
  zones: ParkZone[];
  attractions: PagedResult<ParkItem> | null;
  occurrences: PassportRideOccurrencePage | null;
}

interface PendingIdempotentMutation {
  fingerprint: string;
  key: string;
}

interface PendingAddSubmission {
  request: CreatePassportRideOccurrencesBatchRequest;
  idempotencyKey: string;
  submittedSelections: ReadonlySet<PassportAttractionSelectionDraft>;
}

interface PendingDuplicateSubmission {
  request: CreatePassportRideOccurrencesBatchRequest;
  idempotencyKey: string;
}

type PassportOccurrenceMove = 'first' | 'up' | 'down' | 'last';

@Injectable()
export class PassportVisitEditorStateFacade {
  private static readonly AttractionPageSize: number = 24;
  private static readonly TimelinePageSize: number = 50;

  private readonly visitSignal = signal<PassportVisit | null>(null);
  private readonly parkNameSignal = signal<string>('');
  private readonly zonesSignal = signal<PassportVisitEditorZone[]>([]);
  private readonly attractionsSignal = signal<PassportVisitEditorAttraction[]>([]);
  private readonly attractionPaginationSignal = signal<PaginationContract>({
    currentPage: 1,
    itemsPerPage: PassportVisitEditorStateFacade.AttractionPageSize,
    totalItems: 0,
    totalPages: 0
  });
  private readonly selectedAttractionsSignal = signal<PassportAttractionSelectionDraft[]>([]);
  private readonly occurrencesSignal = signal<PassportRideOccurrence[]>([]);
  private readonly editDraftsSignal = signal<Readonly<Record<string, PassportOccurrenceEditDraft>>>({});
  private readonly nextTimelineCursorSignal = signal<string | null>(null);
  private readonly attractionNamesSignal = signal<Readonly<Record<string, string>>>({});
  private readonly loadingSignal = signal<boolean>(false);
  private readonly attractionsLoadingSignal = signal<boolean>(false);
  private readonly timelineLoadingSignal = signal<boolean>(false);
  private readonly timelineLoadingMoreSignal = signal<boolean>(false);
  private readonly addingSignal = signal<boolean>(false);
  private readonly busyOccurrenceIdsSignal = signal<ReadonlySet<string>>(new Set<string>());
  private readonly loadErrorKeySignal = signal<string | null>(null);
  private readonly attractionErrorKeySignal = signal<string | null>(null);
  private readonly operationErrorKeySignal = signal<string | null>(null);
  private readonly normalizationNoticeSignal = signal<boolean>(false);
  private readonly pendingAddRecoverySignal = signal<boolean>(false);
  private readonly pendingDuplicateRecoveryIdsSignal = signal<ReadonlySet<string>>(new Set<string>());
  private readonly pendingMutations = new Map<string, PendingIdempotentMutation>();
  private readonly pendingDuplicateSubmissions = new Map<string, PendingDuplicateSubmission>();
  private readonly persistedEditFingerprints = new Map<string, string>();
  private readonly pendingTimelineDraftSubmissions = new Map<string, string>();
  private pendingAddSubmission: PendingAddSubmission | null = null;
  private currentVisitId: string | null = null;
  private currentLanguage: string = 'en';
  private currentAttractionSearch: string = '';
  private currentZoneId: string | null = null;
  private visitInstanceGeneration: number = 0;
  private editorLoadGeneration: number = 0;
  private attractionLoadGeneration: number = 0;
  private addSubmissionGeneration: number = 0;
  private timelineGeneration: number = 0;
  private timelineReloadRequestGeneration: number = 0;
  private timelineReloadQueued: boolean = false;

  readonly visit: Signal<PassportVisit | null> = this.visitSignal.asReadonly();
  readonly parkName: Signal<string> = this.parkNameSignal.asReadonly();
  readonly zones: Signal<PassportVisitEditorZone[]> = this.zonesSignal.asReadonly();
  readonly attractions: Signal<PassportVisitEditorAttraction[]> = this.attractionsSignal.asReadonly();
  readonly attractionPagination: Signal<PaginationContract> = this.attractionPaginationSignal.asReadonly();
  readonly selectedAttractions: Signal<PassportAttractionSelectionDraft[]> = this.selectedAttractionsSignal.asReadonly();
  readonly occurrences: Signal<PassportRideOccurrence[]> = this.occurrencesSignal.asReadonly();
  readonly editDrafts: Signal<Readonly<Record<string, PassportOccurrenceEditDraft>>> = this.editDraftsSignal.asReadonly();
  readonly nextTimelineCursor: Signal<string | null> = this.nextTimelineCursorSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly attractionsLoading: Signal<boolean> = this.attractionsLoadingSignal.asReadonly();
  readonly timelineLoading: Signal<boolean> = this.timelineLoadingSignal.asReadonly();
  readonly timelineLoadingMore: Signal<boolean> = this.timelineLoadingMoreSignal.asReadonly();
  readonly adding: Signal<boolean> = this.addingSignal.asReadonly();
  readonly busyOccurrenceIds: Signal<ReadonlySet<string>> = this.busyOccurrenceIdsSignal.asReadonly();
  readonly loadErrorKey: Signal<string | null> = this.loadErrorKeySignal.asReadonly();
  readonly attractionErrorKey: Signal<string | null> = this.attractionErrorKeySignal.asReadonly();
  readonly operationErrorKey: Signal<string | null> = this.operationErrorKeySignal.asReadonly();
  readonly normalizationNotice: Signal<boolean> = this.normalizationNoticeSignal.asReadonly();
  readonly pendingAddRecovery: Signal<boolean> = this.pendingAddRecoverySignal.asReadonly();
  readonly pendingDuplicateRecoveryIds: Signal<ReadonlySet<string>> =
    this.pendingDuplicateRecoveryIdsSignal.asReadonly();
  readonly acceptsLocalTime = computed((): boolean => {
    const visit: PassportVisit | null = this.visitSignal();
    return visit?.date.precision === 'Day' && Boolean(visit.timeZoneId?.trim());
  });
  readonly selectedOccurrenceTotal = computed((): number => this.selectedAttractionsSignal()
    .reduce((total: number, selection: PassportAttractionSelectionDraft): number => total + normalizeCount(selection.count), 0));
  readonly rideCount = computed((): number => this.occurrencesSignal()
    .filter((occurrence: PassportRideOccurrence): boolean => occurrence.countsAsRide).length);
  readonly occurrenceRows = computed((): PassportRideOccurrenceRow[] => {
    const occurrences: PassportRideOccurrence[] = this.occurrencesSignal();
    const names: Readonly<Record<string, string>> = this.attractionNamesSignal();
    const totals: Map<string, number> = new Map<string, number>();
    const seen: Map<string, number> = new Map<string, number>();

    for (const occurrence of occurrences) {
      totals.set(occurrence.parkItemId, (totals.get(occurrence.parkItemId) ?? 0) + 1);
    }

    return occurrences.map((occurrence: PassportRideOccurrence): PassportRideOccurrenceRow => {
      const occurrenceNumber: number = (seen.get(occurrence.parkItemId) ?? 0) + 1;
      seen.set(occurrence.parkItemId, occurrenceNumber);
      return {
        occurrence,
        attractionName: occurrence.target?.name?.trim() || names[occurrence.parkItemId] || '',
        occurrenceNumber,
        occurrenceCount: totals.get(occurrence.parkItemId) ?? 1,
        historicalConsistency: occurrence.historicalConsistency
      };
    });
  });

  constructor(
    @Inject(PASSPORT_VISIT_EDITOR_VISITS_PORT) private readonly visitsApi: PassportVisitEditorVisitsPort,
    @Inject(PASSPORT_VISIT_EDITOR_OCCURRENCES_PORT) private readonly occurrencesApi: PassportVisitEditorOccurrencesPort,
    @Inject(PASSPORT_VISIT_EDITOR_PARKS_PORT) private readonly parksApi: PassportVisitEditorParksPort,
    @Inject(PASSPORT_VISIT_EDITOR_ZONES_PORT) private readonly zonesApi: PassportVisitEditorZonesPort,
    @Inject(PASSPORT_VISIT_EDITOR_ATTRACTIONS_PORT) private readonly attractionsApi: PassportVisitEditorAttractionsPort,
    @Inject(PASSPORT_VISIT_EDITOR_OPERATION_ID_PORT) private readonly operationIds: PassportVisitEditorOperationIdPort,
    private readonly messages: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(visitId: string, language: string): void {
    const normalizedVisitId: string = visitId.trim();
    if (!normalizedVisitId) {
      this.loadErrorKeySignal.set('passport.editor.errors.visitNotFound');
      return;
    }

    this.reset(normalizedVisitId, language);
    const loadGeneration: number = ++this.editorLoadGeneration;
    this.loadingSignal.set(true);
    this.loadVisit(normalizedVisitId, loadGeneration, 1);
  }

  changeLanguage(language: string): void {
    const normalizedLanguage: string = language.trim().toLowerCase() || 'en';
    if (normalizedLanguage === this.currentLanguage) {
      return;
    }

    this.currentLanguage = normalizedLanguage;
    this.invalidateTimelinePagination();
    const loadGeneration: number = ++this.editorLoadGeneration;
    const visit: PassportVisit | null = this.visitSignal();
    this.loadingSignal.set(true);
    this.loadErrorKeySignal.set(null);
    if (visit) {
      this.loadVisitDependencies(visit, loadGeneration, this.attractionPaginationSignal().currentPage);
      return;
    }

    if (this.currentVisitId) {
      this.loadVisit(this.currentVisitId, loadGeneration, 1);
      return;
    }

    this.loadingSignal.set(false);
  }

  retryLoad(): void {
    const visitId: string | null = this.currentVisitId;
    if (!visitId) {
      return;
    }

    this.invalidateTimelinePagination();
    const loadGeneration: number = ++this.editorLoadGeneration;
    const visit: PassportVisit | null = this.visitSignal();
    this.loadingSignal.set(true);
    this.loadErrorKeySignal.set(null);
    if (visit) {
      this.loadVisitDependencies(visit, loadGeneration, this.attractionPaginationSignal().currentPage);
      return;
    }

    this.loadVisit(visitId, loadGeneration, 1);
  }

  private loadVisit(visitId: string, loadGeneration: number, attractionPage: number): void {
    this.visitsApi.getVisit(visitId).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (visit: PassportVisit): void => {
        if (loadGeneration !== this.editorLoadGeneration) {
          return;
        }

        this.loadVisitDependencies(visit, loadGeneration, attractionPage);
      },
      error: (error: unknown): void => {
        if (loadGeneration !== this.editorLoadGeneration) {
          return;
        }

        this.loadingSignal.set(false);
        this.loadErrorKeySignal.set(this.resolveErrorKey(error, 'load'));
      }
    });
  }

  applyAttractionFilters(search: string, zoneId: string | null): void {
    this.currentAttractionSearch = search.trim();
    this.currentZoneId = zoneId?.trim() || null;
    this.loadAttractionPage(1);
  }

  goToAttractionPage(page: number): void {
    const pagination: PaginationContract = this.attractionPaginationSignal();
    const safePage: number = Math.min(Math.max(1, Math.trunc(page)), Math.max(1, pagination.totalPages));
    if (safePage !== pagination.currentPage) {
      this.loadAttractionPage(safePage);
    }
  }

  toggleAttraction(attraction: PassportVisitEditorAttraction): void {
    const selections: PassportAttractionSelectionDraft[] = this.selectedAttractionsSignal();
    const existingIndex: number = selections.findIndex(
      (selection: PassportAttractionSelectionDraft): boolean => selection.parkItemId === attraction.id
    );
    if (existingIndex >= 0) {
      this.selectedAttractionsSignal.set(selections.filter(
        (selection: PassportAttractionSelectionDraft): boolean => selection.parkItemId !== attraction.id
      ));
      return;
    }

    this.rememberAttractionNames([attraction]);
    this.selectedAttractionsSignal.set([...selections, createAttractionSelection(attraction)]);
  }

  updateSelection(parkItemId: string, patch: PassportAttractionSelectionPatch): void {
    this.selectedAttractionsSignal.update((selections: PassportAttractionSelectionDraft[]) => selections.map(
      (selection: PassportAttractionSelectionDraft): PassportAttractionSelectionDraft =>
        selection.parkItemId === parkItemId
          ? {
            ...selection,
            ...patch,
            count: patch.count === undefined ? selection.count : normalizeCount(patch.count)
          }
          : selection
    ));
  }

  clearSelection(): void {
    if (this.pendingAddRecoverySignal()) {
      return;
    }

    this.selectedAttractionsSignal.set([]);
    this.operationErrorKeySignal.set(null);
    this.pendingMutations.delete('add-selection');
  }

  isSelected(parkItemId: string): boolean {
    return this.selectedAttractionsSignal().some(
      (selection: PassportAttractionSelectionDraft): boolean => selection.parkItemId === parkItemId
    );
  }

  addSelected(): void {
    const visitId: string | null = this.currentVisitId;
    const selections: PassportAttractionSelectionDraft[] = this.selectedAttractionsSignal();
    const pendingSubmission: PendingAddSubmission | null = this.pendingAddSubmission;
    if (!visitId || (!pendingSubmission && selections.length === 0) || this.addingSignal()) {
      return;
    }

    const submission: PendingAddSubmission | null = pendingSubmission ?? this.createAddSubmission(selections);
    if (!submission) {
      return;
    }

    const request: CreatePassportRideOccurrencesBatchRequest = submission.request;
    const submittedSelections: ReadonlySet<PassportAttractionSelectionDraft> = submission.submittedSelections;
    const idempotencyKey: string = submission.idempotencyKey;
    this.pendingAddSubmission = submission;
    const addGeneration: number = ++this.addSubmissionGeneration;
    this.operationErrorKeySignal.set(null);
    this.addingSignal.set(true);
    this.occurrencesApi.addBatch(visitId, request, idempotencyKey).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (result: PassportRideOccurrenceMutationResult): void => {
        if (addGeneration !== this.addSubmissionGeneration || visitId !== this.currentVisitId) {
          return;
        }

        this.addingSignal.set(false);
        this.pendingAddSubmission = null;
        this.pendingAddRecoverySignal.set(false);
        this.pendingMutations.delete('add-selection');
        this.selectedAttractionsSignal.update((currentSelections: PassportAttractionSelectionDraft[]) =>
          currentSelections.filter((selection: PassportAttractionSelectionDraft): boolean =>
            !submittedSelections.has(selection))
        );
        this.handleMutationSuccess(result);
        this.showSuccess('passport.editor.messages.added');
      },
      error: (error: unknown): void => {
        if (addGeneration !== this.addSubmissionGeneration || visitId !== this.currentVisitId) {
          return;
        }

        this.addingSignal.set(false);
        if (this.isAmbiguousMutationError(error)) {
          this.pendingAddRecoverySignal.set(true);
        } else {
          this.pendingAddSubmission = null;
          this.pendingAddRecoverySignal.set(false);
        }
        this.handleMutationError(error, 'add-selection');
      }
    });
  }

  private createAddSubmission(selections: PassportAttractionSelectionDraft[]): PendingAddSubmission | null {
    const items: CreatePassportRideOccurrenceBatchItem[] = selections.map(
      (selection: PassportAttractionSelectionDraft): CreatePassportRideOccurrenceBatchItem =>
        mapAttractionSelectionToRequest(selection, this.acceptsLocalTime())
    );
    const totalCount: number = items.reduce(
      (sum: number, item: CreatePassportRideOccurrenceBatchItem): number => sum + item.count,
      0
    );
    if (totalCount > 100) {
      this.operationErrorKeySignal.set('passport.editor.errors.batchTooLarge');
      return null;
    }

    const request: CreatePassportRideOccurrencesBatchRequest = { items };
    return {
      request,
      idempotencyKey: this.resolveIdempotencyKey('add-selection', request),
      submittedSelections: new Set<PassportAttractionSelectionDraft>(selections)
    };
  }

  updateOccurrence(occurrence: PassportRideOccurrence, draft: PassportOccurrenceEditDraft): void {
    const visitId: string | null = this.currentVisitId;
    if (!visitId
      || !occurrence.target
      || occurrence.target.isHistoricalSnapshot
      || occurrence.target.category !== 'Attraction'
      || this.isOccurrenceBusy(occurrence.id)) {
      return;
    }

    const visitGeneration: number = this.visitInstanceGeneration;
    const submittedDraftFingerprint: string = JSON.stringify(draft);
    this.setOccurrenceBusy(occurrence.id, true);
    this.operationErrorKeySignal.set(null);
    this.occurrencesApi.update(
      visitId,
      occurrence.id,
      mapOccurrenceEditToRequest(occurrence, draft, this.acceptsLocalTime())
    ).pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated: PassportRideOccurrence): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        const nextOccurrences: PassportRideOccurrence[] = this.occurrencesSignal().map(
          (candidate: PassportRideOccurrence): PassportRideOccurrence => candidate.id === updated.id
            ? { ...updated, target: updated.target ?? candidate.target }
            : candidate
        );
        const currentDraft: PassportOccurrenceEditDraft | undefined = this.editDraftsSignal()[occurrence.id];
        const draftToPreserve: PassportOccurrenceEditDraft | null = currentDraft
          && JSON.stringify(currentDraft) !== submittedDraftFingerprint
          ? currentDraft
          : null;
        this.setOccurrences(nextOccurrences);
        if (draftToPreserve) {
          this.editDraftsSignal.update((current: Readonly<Record<string, PassportOccurrenceEditDraft>>) => ({
            ...current,
            [occurrence.id]: draftToPreserve
          }));
        }
        this.reloadTimeline();
        this.showSuccess('passport.editor.messages.updated');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.operationErrorKeySignal.set(this.resolveErrorKey(error, 'operation'));
        if (this.isAmbiguousMutationError(error)) {
          this.pendingTimelineDraftSubmissions.set(occurrence.id, submittedDraftFingerprint);
        }
        this.reloadTimeline();
      }
    });
  }

  updateOccurrenceDraft(occurrenceId: string, patch: Partial<PassportOccurrenceEditDraft>): void {
    this.editDraftsSignal.update((current: Readonly<Record<string, PassportOccurrenceEditDraft>>) => ({
      ...current,
      [occurrenceId]: {
        ...(current[occurrenceId] ?? {
          status: 'Completed',
          localTime: '',
          isApproximate: false,
          privateNote: '',
          confirmHistoricalConflict: false
        }),
        ...patch
      }
    }));
  }

  duplicateOccurrence(occurrence: PassportRideOccurrence): void {
    const visitId: string | null = this.currentVisitId;
    const operationName: string = `duplicate:${occurrence.id}`;
    const pendingSubmission: PendingDuplicateSubmission | undefined =
      this.pendingDuplicateSubmissions.get(occurrence.id);
    if (!visitId
      || (!pendingSubmission && (!occurrence.target
        || occurrence.target.isHistoricalSnapshot
        || occurrence.target.category !== 'Attraction'))
      || this.isOccurrenceBusy(occurrence.id)) {
      return;
    }

    const visitGeneration: number = this.visitInstanceGeneration;
    const submission: PendingDuplicateSubmission = pendingSubmission
      ?? this.createDuplicateSubmission(occurrence, operationName);
    this.pendingDuplicateSubmissions.set(occurrence.id, submission);
    this.setOccurrenceBusy(occurrence.id, true);
    this.operationErrorKeySignal.set(null);
    this.occurrencesApi.addBatch(visitId, submission.request, submission.idempotencyKey).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (result: PassportRideOccurrenceMutationResult): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.pendingDuplicateSubmissions.delete(occurrence.id);
        this.setDuplicateRecovery(occurrence.id, false);
        this.pendingMutations.delete(operationName);
        this.handleMutationSuccess(result);
        this.showSuccess('passport.editor.messages.duplicated');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        if (this.isAmbiguousMutationError(error)) {
          this.setDuplicateRecovery(occurrence.id, true);
        } else {
          this.pendingDuplicateSubmissions.delete(occurrence.id);
          this.setDuplicateRecovery(occurrence.id, false);
        }
        this.handleMutationError(error, operationName);
      }
    });
  }

  private createDuplicateSubmission(
    occurrence: PassportRideOccurrence,
    operationName: string
  ): PendingDuplicateSubmission {
    const item: CreatePassportRideOccurrenceBatchItem = {
      parkItemId: occurrence.parkItemId,
      moment: occurrence.moment,
      status: occurrence.status,
      privateNote: occurrence.privateNote,
      confirmHistoricalConflict: occurrence.historicalConsistency === 'ConfirmedConflict',
      count: 1
    };
    const request: CreatePassportRideOccurrencesBatchRequest = { items: [item] };
    return {
      request,
      idempotencyKey: this.resolveIdempotencyKey(operationName, request)
    };
  }

  deleteOccurrence(occurrence: PassportRideOccurrence): void {
    const visitId: string | null = this.currentVisitId;
    if (!visitId || this.isOccurrenceBusy(occurrence.id)) {
      return;
    }

    const visitGeneration: number = this.visitInstanceGeneration;
    this.setOccurrenceBusy(occurrence.id, true);
    this.operationErrorKeySignal.set(null);
    this.occurrencesApi.delete(visitId, occurrence.id, occurrence.version).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.pendingDuplicateSubmissions.delete(occurrence.id);
        this.setDuplicateRecovery(occurrence.id, false);
        this.pendingMutations.delete(`duplicate:${occurrence.id}`);
        this.removeOccurrenceDraft(occurrence.id);
        this.setOccurrences(this.occurrencesSignal().filter(
          (candidate: PassportRideOccurrence): boolean => candidate.id !== occurrence.id
        ));
        this.reloadTimeline();
        this.showSuccess('passport.editor.messages.deleted');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.operationErrorKeySignal.set(this.resolveErrorKey(error, 'operation'));
        this.reloadTimeline();
      }
    });
  }

  moveOccurrence(occurrence: PassportRideOccurrence, move: PassportOccurrenceMove): void {
    const visitId: string | null = this.currentVisitId;
    const occurrences: PassportRideOccurrence[] = this.occurrencesSignal();
    const index: number = occurrences.findIndex(
      (candidate: PassportRideOccurrence): boolean => candidate.id === occurrence.id
    );
    if (!visitId || index < 0 || this.isOccurrenceBusy(occurrence.id)) {
      return;
    }

    const placementAndAnchor: { placement: PassportRideOccurrencePlacement; anchorOccurrenceId: string | null } | null =
      this.resolvePlacement(occurrences, index, move);
    if (!placementAndAnchor) {
      return;
    }

    const visitGeneration: number = this.visitInstanceGeneration;
    const request: ReorderPassportRideOccurrenceRequest = {
      occurrenceId: occurrence.id,
      expectedVersion: occurrence.version,
      ...placementAndAnchor
    };
    const operationName: string = `reorder:${occurrence.id}`;
    const key: string = this.resolveIdempotencyKey(operationName, request);
    this.setOccurrenceBusy(occurrence.id, true);
    this.operationErrorKeySignal.set(null);
    this.occurrencesApi.reorder(visitId, request, key).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (result: PassportRideOccurrenceMutationResult): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.pendingMutations.delete(operationName);
        this.handleMutationSuccess(result);
        this.showSuccess('passport.editor.messages.reordered');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.handleMutationError(error, operationName);
      }
    });
  }

  loadMoreTimeline(): void {
    const visitId: string | null = this.currentVisitId;
    const cursor: string | null = this.nextTimelineCursorSignal();
    if (!visitId || !cursor || this.timelineLoadingSignal() || this.timelineLoadingMoreSignal()) {
      return;
    }

    const generation: number = this.timelineGeneration;
    this.timelineLoadingMoreSignal.set(true);
    this.occurrencesApi.list(visitId, cursor, PassportVisitEditorStateFacade.TimelinePageSize).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (page: PassportRideOccurrencePage): void => {
        if (generation !== this.timelineGeneration
          || cursor !== this.nextTimelineCursorSignal()) {
          return;
        }

        this.timelineLoadingMoreSignal.set(false);
        this.setTimelineOccurrences([...this.occurrencesSignal(), ...page.items]);
        this.nextTimelineCursorSignal.set(page.nextCursor);
      },
      error: (error: unknown): void => {
        if (generation !== this.timelineGeneration) {
          return;
        }

        this.timelineLoadingMoreSignal.set(false);
        this.operationErrorKeySignal.set(this.resolveErrorKey(error, 'timeline'));
      }
    });
  }

  reloadTimeline(): void {
    const visitId: string | null = this.currentVisitId;
    if (!visitId) {
      return;
    }

    if (this.timelineLoadingSignal()) {
      this.timelineReloadQueued = true;
      this.invalidateTimelinePagination();
      return;
    }

    this.invalidateTimelinePagination();
    const visitGeneration: number = this.visitInstanceGeneration;
    const generation: number = this.timelineGeneration;
    const reloadRequestGeneration: number = ++this.timelineReloadRequestGeneration;
    this.timelineLoadingSignal.set(true);
    this.occurrencesApi.list(visitId, null, PassportVisitEditorStateFacade.TimelinePageSize).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (page: PassportRideOccurrencePage): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)
          || reloadRequestGeneration !== this.timelineReloadRequestGeneration) {
          return;
        }

        this.timelineLoadingSignal.set(false);
        if (generation === this.timelineGeneration) {
          this.setTimelineOccurrences(page.items);
          this.nextTimelineCursorSignal.set(page.nextCursor);
        }
        this.runQueuedTimelineReload();
      },
      error: (error: unknown): void => {
        if (!this.isCurrentVisitInstance(visitId, visitGeneration)
          || reloadRequestGeneration !== this.timelineReloadRequestGeneration) {
          return;
        }

        this.timelineLoadingSignal.set(false);
        if (generation === this.timelineGeneration) {
          this.operationErrorKeySignal.set(this.resolveErrorKey(error, 'timeline'));
        }
        this.runQueuedTimelineReload();
      }
    });
  }

  dismissNormalizationNotice(): void {
    this.normalizationNoticeSignal.set(false);
  }

  private loadVisitDependencies(visit: PassportVisit, loadGeneration: number, attractionPage: number): void {
    const parkId: string = visit.parkId;
    const timelineGeneration: number = this.timelineGeneration;
    const attractionGeneration: number = ++this.attractionLoadGeneration;
    this.attractionsLoadingSignal.set(true);
    const dependencies: Observable<InitialVisitEditorData> = forkJoin({
      park: this.parksApi.getParkById(parkId, { closedFilter: 'all' }).pipe(
        catchError(() => of<Park | null>(null))
      ),
      zones: this.zonesApi.getParkZonesByParkId(parkId).pipe(
        catchError(() => of<ParkZone[]>([]))
      ),
      attractions: this.attractionsApi.getParkItemsByParkIdPage(
        parkId,
        attractionPage,
        PassportVisitEditorStateFacade.AttractionPageSize,
        {
          closedFilter: 'all',
          category: 'Attraction',
          search: this.currentAttractionSearch || null,
          zoneId: this.currentZoneId
        },
        { closedFilter: 'all' }
      ).pipe(catchError(() => of<PagedResult<ParkItem> | null>(null))),
      occurrences: this.occurrencesApi.list(
        visit.id,
        null,
        PassportVisitEditorStateFacade.TimelinePageSize
      ).pipe(catchError((error: unknown): Observable<PassportRideOccurrencePage | null> =>
        timelineGeneration !== this.timelineGeneration
          ? of<PassportRideOccurrencePage | null>(null)
          : throwError(() => error)))
    });

    dependencies.pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data: InitialVisitEditorData): void => {
        if (loadGeneration !== this.editorLoadGeneration) {
          return;
        }

        this.visitSignal.set(visit);
        this.parkNameSignal.set(data.park?.name?.trim() || visit.parkId);
        this.zonesSignal.set(data.zones
          .map((zone: ParkZone): PassportVisitEditorZone | null => mapParkZoneToVisitEditorZone(zone, this.currentLanguage))
          .filter((zone: PassportVisitEditorZone | null): zone is PassportVisitEditorZone => zone !== null));
        if (data.attractions && attractionGeneration === this.attractionLoadGeneration) {
          this.attractionsLoadingSignal.set(false);
          this.attractionErrorKeySignal.set(null);
          this.applyAttractionPage(data.attractions);
        } else if (attractionGeneration === this.attractionLoadGeneration) {
          this.attractionsLoadingSignal.set(false);
          this.attractionErrorKeySignal.set('passport.editor.errors.attractions');
        }
        if (data.occurrences && timelineGeneration === this.timelineGeneration) {
          this.timelineReloadRequestGeneration += 1;
          this.timelineLoadingSignal.set(false);
          this.setTimelineOccurrences(data.occurrences.items);
          this.nextTimelineCursorSignal.set(data.occurrences.nextCursor);
          this.runQueuedTimelineReload();
        }
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        if (loadGeneration !== this.editorLoadGeneration) {
          return;
        }

        if (attractionGeneration === this.attractionLoadGeneration) {
          this.attractionsLoadingSignal.set(false);
        }
        if (timelineGeneration === this.timelineGeneration) {
          this.timelineReloadRequestGeneration += 1;
          this.timelineLoadingSignal.set(false);
        }
        this.loadingSignal.set(false);
        if (timelineGeneration !== this.timelineGeneration) {
          return;
        }

        this.loadErrorKeySignal.set(this.resolveErrorKey(error, 'load'));
        this.runQueuedTimelineReload();
      }
    });
  }

  private loadAttractionPage(page: number): void {
    const parkId: string | undefined = this.visitSignal()?.parkId;
    if (!parkId || this.attractionsLoadingSignal()) {
      return;
    }

    const attractionGeneration: number = ++this.attractionLoadGeneration;
    this.attractionsLoadingSignal.set(true);
    this.attractionErrorKeySignal.set(null);
    this.attractionsApi.getParkItemsByParkIdPage(
      parkId,
      page,
      PassportVisitEditorStateFacade.AttractionPageSize,
      {
        closedFilter: 'all',
        category: 'Attraction',
        search: this.currentAttractionSearch || null,
        zoneId: this.currentZoneId
      },
      { closedFilter: 'all' }
    ).pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: PagedResult<ParkItem>): void => {
        if (attractionGeneration !== this.attractionLoadGeneration) {
          return;
        }

        this.attractionsLoadingSignal.set(false);
        this.applyAttractionPage(result);
      },
      error: (): void => {
        if (attractionGeneration !== this.attractionLoadGeneration) {
          return;
        }

        this.attractionsLoadingSignal.set(false);
        this.attractionErrorKeySignal.set('passport.editor.errors.attractions');
      }
    });
  }

  private applyAttractionPage(result: PagedResult<ParkItem>): void {
    const attractions: PassportVisitEditorAttraction[] = result.items
      .map(mapParkItemToVisitEditorAttraction)
      .filter((item: PassportVisitEditorAttraction | null): item is PassportVisitEditorAttraction => item !== null);
    this.attractionsSignal.set(attractions);
    this.attractionPaginationSignal.set(result.pagination);
    this.rememberAttractionNames(attractions);
  }

  private rememberAttractionNames(attractions: readonly PassportVisitEditorAttraction[]): void {
    if (attractions.length === 0) {
      return;
    }

    this.attractionNamesSignal.update((current: Readonly<Record<string, string>>) => {
      const next: Record<string, string> = { ...current };
      for (const attraction of attractions) {
        next[attraction.id] = attraction.name;
      }

      return next;
    });
  }

  private setOccurrences(occurrences: PassportRideOccurrence[]): void {
    const currentDrafts: Readonly<Record<string, PassportOccurrenceEditDraft>> = this.editDraftsSignal();
    const nextDrafts: Record<string, PassportOccurrenceEditDraft> = { ...currentDrafts };
    const nextFingerprints: Map<string, string> = new Map<string, string>(this.persistedEditFingerprints);

    for (const occurrence of occurrences) {
      const persistedDraft: PassportOccurrenceEditDraft = mapOccurrenceToEditDraft(occurrence);
      const persistedFingerprint: string = JSON.stringify(persistedDraft);
      const currentDraft: PassportOccurrenceEditDraft | undefined = currentDrafts[occurrence.id];
      nextDrafts[occurrence.id] = currentDraft
        && this.persistedEditFingerprints.get(occurrence.id) === persistedFingerprint
          ? currentDraft
          : persistedDraft;
      nextFingerprints.set(occurrence.id, persistedFingerprint);
    }

    this.persistedEditFingerprints.clear();
    for (const [occurrenceId, fingerprint] of nextFingerprints) {
      this.persistedEditFingerprints.set(occurrenceId, fingerprint);
    }

    this.editDraftsSignal.set(nextDrafts);
    this.occurrencesSignal.set(occurrences);
  }

  private setTimelineOccurrences(occurrences: PassportRideOccurrence[]): void {
    const draftsToPreserve: Record<string, PassportOccurrenceEditDraft> = {};
    const loadedOccurrenceIds: Set<string> = new Set<string>(
      occurrences.map((occurrence: PassportRideOccurrence): string => occurrence.id)
    );
    for (const [occurrenceId, submittedFingerprint] of this.pendingTimelineDraftSubmissions) {
      if (!loadedOccurrenceIds.has(occurrenceId)) {
        continue;
      }

      const currentDraft: PassportOccurrenceEditDraft | undefined = this.editDraftsSignal()[occurrenceId];
      if (currentDraft && JSON.stringify(currentDraft) !== submittedFingerprint) {
        draftsToPreserve[occurrenceId] = currentDraft;
      }
    }

    this.setOccurrences(occurrences);
    if (Object.keys(draftsToPreserve).length > 0) {
      this.editDraftsSignal.update((current: Readonly<Record<string, PassportOccurrenceEditDraft>>) => ({
        ...current,
        ...draftsToPreserve
      }));
    }
    for (const occurrenceId of loadedOccurrenceIds) {
      this.pendingTimelineDraftSubmissions.delete(occurrenceId);
    }
  }

  private removeOccurrenceDraft(occurrenceId: string): void {
    this.persistedEditFingerprints.delete(occurrenceId);
    this.editDraftsSignal.update((current: Readonly<Record<string, PassportOccurrenceEditDraft>>) => {
      const next: Record<string, PassportOccurrenceEditDraft> = { ...current };
      delete next[occurrenceId];
      return next;
    });
  }

  private invalidateTimelinePagination(): void {
    this.timelineGeneration += 1;
    this.timelineLoadingMoreSignal.set(false);
  }

  private runQueuedTimelineReload(): void {
    if (!this.timelineReloadQueued) {
      return;
    }

    this.timelineReloadQueued = false;
    this.reloadTimeline();
  }

  private handleMutationSuccess(result: PassportRideOccurrenceMutationResult): void {
    if (result.wasOrderNormalized) {
      this.normalizationNoticeSignal.set(true);
    }

    this.reloadTimeline();
  }

  private handleMutationError(error: unknown, operationName?: string): void {
    const errorKey: string = this.resolveErrorKey(error, 'operation');
    this.operationErrorKeySignal.set(errorKey);
    if (operationName && errorKey === 'passport.editor.errors.idempotencyConflict') {
      this.pendingMutations.delete(operationName);
    }

    if (errorKey === 'passport.editor.errors.versionConflict') {
      this.reloadTimeline();
    }
  }

  private resolveErrorKey(error: unknown, context: 'load' | 'timeline' | 'operation'): string {
    const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
    if (errorCode === 'visit.not-found') {
      return 'passport.editor.errors.visitNotFound';
    }

    if (errorCode === 'ride-occurrence.historical-conflict-confirmation-required') {
      return 'passport.editor.errors.historicalConflict';
    }

    if (errorCode === 'ride-occurrence.version-conflict') {
      return 'passport.editor.errors.versionConflict';
    }

    if (errorCode === 'ride-occurrence.idempotency-key-conflict') {
      return 'passport.editor.errors.idempotencyConflict';
    }

    if (errorCode === 'ride-occurrence.target-not-found') {
      return 'passport.editor.errors.attractionUnavailable';
    }

    if (errorCode === 'ride-occurrence.private-note-too-long') {
      return 'passport.editor.errors.noteTooLong';
    }

    if (errorCode === 'ride-occurrence.time-requires-exact-day-and-time-zone') {
      return 'passport.editor.errors.timeUnavailable';
    }

    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'passport.editor.errors.network';
    }

    if (context === 'load') {
      return 'passport.editor.errors.load';
    }

    if (context === 'timeline') {
      return 'passport.editor.errors.timeline';
    }

    return 'passport.editor.errors.operation';
  }

  private isAmbiguousMutationError(error: unknown): boolean {
    return error instanceof HttpErrorResponse
      && (error.status === 0 || error.status === 408 || error.status >= 500);
  }

  private resolveIdempotencyKey(operationName: string, request: unknown): string {
    const fingerprint: string = JSON.stringify(request);
    const pending: PendingIdempotentMutation | undefined = this.pendingMutations.get(operationName);
    if (pending?.fingerprint === fingerprint) {
      return pending.key;
    }

    const key: string = this.operationIds.create();
    this.pendingMutations.set(operationName, { fingerprint, key });
    return key;
  }

  private resolvePlacement(
    occurrences: readonly PassportRideOccurrence[],
    index: number,
    move: PassportOccurrenceMove
  ): { placement: PassportRideOccurrencePlacement; anchorOccurrenceId: string | null } | null {
    if (move === 'first') {
      return index === 0 ? null : { placement: 'First', anchorOccurrenceId: null };
    }

    if (move === 'last') {
      return index === occurrences.length - 1 && !this.nextTimelineCursorSignal()
        ? null
        : { placement: 'Last', anchorOccurrenceId: null };
    }

    if (move === 'up') {
      return index <= 0 ? null : { placement: 'Before', anchorOccurrenceId: occurrences[index - 1].id };
    }

    return index >= occurrences.length - 1
      ? null
      : { placement: 'After', anchorOccurrenceId: occurrences[index + 1].id };
  }

  private isOccurrenceBusy(occurrenceId: string): boolean {
    return this.busyOccurrenceIdsSignal().has(occurrenceId);
  }

  private setOccurrenceBusy(occurrenceId: string, busy: boolean): void {
    this.busyOccurrenceIdsSignal.update((current: ReadonlySet<string>) => {
      const next: Set<string> = new Set<string>(current);
      if (busy) {
        next.add(occurrenceId);
      } else {
        next.delete(occurrenceId);
      }

      return next;
    });
  }

  private isCurrentVisitInstance(visitId: string, visitGeneration: number): boolean {
    return visitId === this.currentVisitId && visitGeneration === this.visitInstanceGeneration;
  }

  private setDuplicateRecovery(occurrenceId: string, pending: boolean): void {
    this.pendingDuplicateRecoveryIdsSignal.update((current: ReadonlySet<string>) => {
      const next: Set<string> = new Set<string>(current);
      if (pending) {
        next.add(occurrenceId);
      } else {
        next.delete(occurrenceId);
      }

      return next;
    });
  }

  private showSuccess(messageKey: string): void {
    this.messages.add(
      'success',
      this.translateService.instant('common.success'),
      this.translateService.instant(messageKey)
    );
  }

  private reset(visitId: string, language: string): void {
    this.visitInstanceGeneration += 1;
    this.currentVisitId = visitId;
    this.currentLanguage = language.trim().toLowerCase() || 'en';
    this.currentAttractionSearch = '';
    this.currentZoneId = null;
    this.visitSignal.set(null);
    this.parkNameSignal.set('');
    this.zonesSignal.set([]);
    this.attractionsSignal.set([]);
    this.selectedAttractionsSignal.set([]);
    this.persistedEditFingerprints.clear();
    this.editDraftsSignal.set({});
    this.occurrencesSignal.set([]);
    this.nextTimelineCursorSignal.set(null);
    this.attractionNamesSignal.set({});
    this.loadErrorKeySignal.set(null);
    this.attractionErrorKeySignal.set(null);
    this.operationErrorKeySignal.set(null);
    this.normalizationNoticeSignal.set(false);
    this.attractionsLoadingSignal.set(false);
    this.timelineLoadingSignal.set(false);
    this.timelineLoadingMoreSignal.set(false);
    this.addingSignal.set(false);
    this.busyOccurrenceIdsSignal.set(new Set<string>());
    this.pendingMutations.clear();
    this.pendingDuplicateSubmissions.clear();
    this.pendingTimelineDraftSubmissions.clear();
    this.pendingAddSubmission = null;
    this.pendingAddRecoverySignal.set(false);
    this.pendingDuplicateRecoveryIdsSignal.set(new Set<string>());
    this.addSubmissionGeneration += 1;
    this.attractionLoadGeneration += 1;
    this.timelineReloadRequestGeneration += 1;
    this.invalidateTimelinePagination();
    this.timelineReloadQueued = false;
  }
}
