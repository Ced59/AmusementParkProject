import { HttpErrorResponse } from '@angular/common/http';
import { computed, DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { catchError, forkJoin, map, Observable, of, switchMap, take, throwError } from 'rxjs';

import {
  CreatePassportRideOccurrenceBatchItem,
  CreatePassportRideOccurrencesBatchRequest,
  PassportRideAssessment,
  PassportRideAssessmentDraft,
  PassportRideOccurrence,
  PassportRideOccurrenceMutationResult,
  PassportRideOccurrencePage,
  PassportRideOccurrencePlacement,
  PassportVisitRideTargetEvaluation,
  ReorderPassportRideOccurrenceRequest,
  UpsertPassportRideAssessmentRequest
} from '@app/models/passport/passport-ride-occurrence.models';
import {
  DeletePassportVisitRequest,
  PassportVisit,
  PassportVisitDeletionPreview,
  PassportVisitDeletionReceipt,
  PassportVisitParkAssessment,
  PassportVisitStatus,
  UpdatePassportVisitRequest,
  UpsertPassportVisitParkAssessmentRequest
} from '@app/models/passport/passport-visit.models';
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
  createPassportVisitMetadataDraft,
  mapPassportVisitMetadataDraft,
  PassportVisitMetadataMappingResult
} from '../mappers/passport-visit-metadata.mapper';
import {
  PassportAttractionSelectionDraft,
  PassportAttractionSelectionPatch,
  PassportOccurrenceEditDraft,
  PassportRideOccurrenceRow,
  PassportVisitEditorAttraction,
  PassportVisitMetadataDraft,
  PassportVisitParkAssessmentDraft,
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
  attractions: EvaluatedAttractionPage | null;
  occurrences: PassportRideOccurrencePage | null;
}

interface EvaluatedAttractionPage {
  page: PagedResult<ParkItem>;
  evaluations: PassportVisitRideTargetEvaluation[];
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

  private static readonly TargetEvaluationBatchSize: number = 100;
  private static readonly TimelinePageSize: number = 50;

  private readonly visitSignal = signal<PassportVisit | null>(null);
  private readonly metadataDraftSignal = signal<PassportVisitMetadataDraft>({
    precision: 'Day',
    year: null,
    month: null,
    day: null,
    isApproximate: false,
    timeZoneId: '',
    serviceDayConvention: 'VisitStartLocalDate',
    title: '',
    privateNote: ''
  });
  private readonly persistedMetadataFingerprintSignal = signal<string>('');
  private readonly visitMutationSavingSignal = signal<boolean>(false);
  private readonly visitMutationErrorKeySignal = signal<string | null>(null);
  private readonly deletionPreviewSignal = signal<PassportVisitDeletionPreview | null>(null);
  private readonly deletionPreviewLoadingSignal = signal<boolean>(false);
  private readonly deletionSubmittingSignal = signal<boolean>(false);
  private readonly deletionErrorKeySignal = signal<string | null>(null);
  private readonly deletedVisitIdSignal = signal<string | null>(null);
  private readonly assessmentDraftSignal = signal<PassportVisitParkAssessmentDraft>({
    value: null,
    privateComment: ''
  });
  private readonly persistedAssessmentFingerprintSignal = signal<string>(this.assessmentFingerprint(null));
  private readonly assessmentSavingSignal = signal<boolean>(false);
  private readonly assessmentErrorKeySignal = signal<string | null>(null);
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
  private readonly rideAssessmentDraftsSignal = signal<Readonly<Record<string, PassportRideAssessmentDraft>>>({});
  private readonly rideAssessmentErrorKeysSignal = signal<Readonly<Record<string, string | null>>>({});
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
  private readonly targetEvaluationsStaleSignal = signal<boolean>(false);
  private readonly timelineConsistencyStaleSignal = signal<boolean>(false);
  private readonly operationErrorKeySignal = signal<string | null>(null);
  private readonly normalizationNoticeSignal = signal<boolean>(false);
  private readonly pendingAddRecoverySignal = signal<boolean>(false);
  private readonly pendingDuplicateRecoveryIdsSignal = signal<ReadonlySet<string>>(new Set<string>());
  private readonly pendingMutations = new Map<string, PendingIdempotentMutation>();
  private readonly pendingDuplicateSubmissions = new Map<string, PendingDuplicateSubmission>();
  private readonly persistedEditFingerprints = new Map<string, string>();
  private readonly persistedRideAssessmentFingerprints = new Map<string, string>();
  private readonly rideAssessmentMutationGenerations = new Map<string, number>();
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
  private assessmentMutationGeneration: number = 0;
  private visitMutationGeneration: number = 0;

  readonly visit: Signal<PassportVisit | null> = this.visitSignal.asReadonly();
  readonly metadataDraft: Signal<PassportVisitMetadataDraft> = this.metadataDraftSignal.asReadonly();
  readonly visitMutationSaving: Signal<boolean> = this.visitMutationSavingSignal.asReadonly();
  readonly visitMutationErrorKey: Signal<string | null> = this.visitMutationErrorKeySignal.asReadonly();
  readonly deletionPreview: Signal<PassportVisitDeletionPreview | null> = this.deletionPreviewSignal.asReadonly();
  readonly deletionPreviewLoading: Signal<boolean> = this.deletionPreviewLoadingSignal.asReadonly();
  readonly deletionSubmitting: Signal<boolean> = this.deletionSubmittingSignal.asReadonly();
  readonly deletionErrorKey: Signal<string | null> = this.deletionErrorKeySignal.asReadonly();
  readonly deletedVisitId: Signal<string | null> = this.deletedVisitIdSignal.asReadonly();
  readonly canEditVisit = computed((): boolean =>
    this.visitSignal()?.status === 'Draft' && !this.visitMutationSavingSignal());
  readonly metadataHasChanges = computed((): boolean =>
    this.metadataDraftFingerprint(this.metadataDraftSignal()) !== this.persistedMetadataFingerprintSignal());
  readonly temporalMetadataHasChanges = computed((): boolean => {
    const visit: PassportVisit | null = this.visitSignal();
    return visit !== null
      && this.temporalMetadataDraftFingerprint(this.metadataDraftSignal())
        !== this.temporalMetadataDraftFingerprint(createPassportVisitMetadataDraft(visit));
  });
  readonly metadataCanSave = computed((): boolean =>
    this.canEditVisit() && this.metadataHasChanges());
  readonly assessmentDraft: Signal<PassportVisitParkAssessmentDraft> = this.assessmentDraftSignal.asReadonly();
  readonly assessmentSaving: Signal<boolean> = this.assessmentSavingSignal.asReadonly();
  readonly assessmentErrorKey: Signal<string | null> = this.assessmentErrorKeySignal.asReadonly();
  readonly assessmentHasChanges = computed((): boolean =>
    this.assessmentDraftFingerprint(this.assessmentDraftSignal()) !== this.persistedAssessmentFingerprintSignal());
  readonly assessmentCanSave = computed((): boolean =>
    this.assessmentDraftSignal().value !== null
    && this.canEditVisit()
    && !this.assessmentSavingSignal()
    && this.assessmentHasChanges());
  readonly hasUnsavedAssessmentChanges = computed((): boolean => {
    const drafts: Readonly<Record<string, PassportRideAssessmentDraft>> = this.rideAssessmentDraftsSignal();
    return this.assessmentHasChanges()
      || Object.entries(drafts).some(
        ([occurrenceId, draft]: [string, PassportRideAssessmentDraft]): boolean =>
          this.rideAssessmentDraftFingerprint(draft)
            !== this.persistedRideAssessmentFingerprints.get(occurrenceId));
  });
  readonly hasUnsavedOccurrenceChanges = computed((): boolean => {
    const drafts: Readonly<Record<string, PassportOccurrenceEditDraft>> = this.editDraftsSignal();
    return this.selectedAttractionsSignal().length > 0
      || Object.entries(drafts).some(
        ([occurrenceId, draft]: [string, PassportOccurrenceEditDraft]): boolean =>
          JSON.stringify(draft) !== this.persistedEditFingerprints.get(occurrenceId));
  });
  readonly hasUnsavedStatusTransitionChanges = computed((): boolean =>
    this.metadataHasChanges()
    || this.hasUnsavedAssessmentChanges()
    || this.hasUnsavedOccurrenceChanges());
  readonly parkName: Signal<string> = this.parkNameSignal.asReadonly();
  readonly zones: Signal<PassportVisitEditorZone[]> = this.zonesSignal.asReadonly();
  readonly attractions: Signal<PassportVisitEditorAttraction[]> = this.attractionsSignal.asReadonly();
  readonly attractionPagination: Signal<PaginationContract> = this.attractionPaginationSignal.asReadonly();
  readonly selectedAttractions: Signal<PassportAttractionSelectionDraft[]> = this.selectedAttractionsSignal.asReadonly();
  readonly occurrences: Signal<PassportRideOccurrence[]> = this.occurrencesSignal.asReadonly();
  readonly editDrafts: Signal<Readonly<Record<string, PassportOccurrenceEditDraft>>> = this.editDraftsSignal.asReadonly();
  readonly rideAssessmentDrafts: Signal<Readonly<Record<string, PassportRideAssessmentDraft>>> =
    this.rideAssessmentDraftsSignal.asReadonly();
  readonly rideAssessmentErrorKeys: Signal<Readonly<Record<string, string | null>>> =
    this.rideAssessmentErrorKeysSignal.asReadonly();
  readonly nextTimelineCursor: Signal<string | null> = this.nextTimelineCursorSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly attractionsLoading: Signal<boolean> = this.attractionsLoadingSignal.asReadonly();
  readonly timelineLoading: Signal<boolean> = this.timelineLoadingSignal.asReadonly();
  readonly timelineLoadingMore: Signal<boolean> = this.timelineLoadingMoreSignal.asReadonly();
  readonly adding: Signal<boolean> = this.addingSignal.asReadonly();
  readonly busyOccurrenceIds: Signal<ReadonlySet<string>> = this.busyOccurrenceIdsSignal.asReadonly();
  readonly loadErrorKey: Signal<string | null> = this.loadErrorKeySignal.asReadonly();
  readonly attractionErrorKey: Signal<string | null> = this.attractionErrorKeySignal.asReadonly();
  readonly targetEvaluationsStale: Signal<boolean> = this.targetEvaluationsStaleSignal.asReadonly();
  readonly timelineConsistencyStale: Signal<boolean> = this.timelineConsistencyStaleSignal.asReadonly();
  readonly operationErrorKey: Signal<string | null> = this.operationErrorKeySignal.asReadonly();
  readonly normalizationNotice: Signal<boolean> = this.normalizationNoticeSignal.asReadonly();
  readonly pendingAddRecovery: Signal<boolean> = this.pendingAddRecoverySignal.asReadonly();
  readonly pendingDuplicateRecoveryIds: Signal<ReadonlySet<string>> =
    this.pendingDuplicateRecoveryIdsSignal.asReadonly();
  readonly selectionCanSubmit = computed((): boolean =>
    !this.addingSignal()
    && !this.attractionsLoadingSignal()
    && !this.targetEvaluationsStaleSignal()
    && (this.pendingAddRecoverySignal() || !this.temporalMetadataHasChanges())
    && this.selectedAttractionsSignal().every(
      (selection: PassportAttractionSelectionDraft): boolean =>
        selection.historicalConsistency !== 'ConfirmedConflict'
        || selection.confirmHistoricalConflict));
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

  retryTargetEvaluations(): void {
    const visit: PassportVisit | null = this.visitSignal();
    if (!visit || this.attractionsLoadingSignal()) {
      return;
    }

    this.refreshLoadedTargetEvaluations(visit.id);
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
    if (!visitId
      || (!pendingSubmission && selections.length === 0)
      || this.addingSignal()
      || this.targetEvaluationsStaleSignal()
      || (!pendingSubmission && this.temporalMetadataHasChanges())) {
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
    if (!visitId || !this.canUpdateOccurrence(occurrence, draft)) {
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

  canUpdateOccurrence(occurrence: PassportRideOccurrence, draft: PassportOccurrenceEditDraft): boolean {
    return this.currentVisitId !== null
      && occurrence.target != null
      && !occurrence.target.isHistoricalSnapshot
      && occurrence.target.category === 'Attraction'
      && !this.timelineConsistencyStaleSignal()
      && !this.isOccurrenceBusy(occurrence.id)
      && (occurrence.historicalConsistency !== 'ConfirmedConflict' || draft.confirmHistoricalConflict);
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

  canDuplicateOccurrence(occurrence: PassportRideOccurrence): boolean {
    const hasPendingSubmission: boolean = this.pendingDuplicateSubmissions.has(occurrence.id);
    const conflictConfirmed: boolean = this.editDraftsSignal()[occurrence.id]?.confirmHistoricalConflict
      ?? occurrence.historicalConflictConfirmed
      ?? false;
    return this.currentVisitId !== null
      && !this.timelineConsistencyStaleSignal()
      && !this.isOccurrenceBusy(occurrence.id)
      && (hasPendingSubmission || (occurrence.target != null
        && !occurrence.target.isHistoricalSnapshot
        && occurrence.target.category === 'Attraction'
        && (occurrence.historicalConsistency !== 'ConfirmedConflict' || conflictConfirmed)));
  }

  duplicateOccurrence(occurrence: PassportRideOccurrence): void {
    const visitId: string | null = this.currentVisitId;
    if (!visitId || !this.canDuplicateOccurrence(occurrence)) {
      return;
    }

    const operationName: string = `duplicate:${occurrence.id}`;
    const pendingSubmission: PendingDuplicateSubmission | undefined =
      this.pendingDuplicateSubmissions.get(occurrence.id);
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

  updateVisitMetadataDraft(patch: Partial<PassportVisitMetadataDraft>): void {
    this.metadataDraftSignal.update((current: PassportVisitMetadataDraft) => {
      let next: PassportVisitMetadataDraft = { ...current, ...patch };
      if (next.precision === 'Year') {
        next = { ...next, month: null, day: null };
      } else if (next.precision === 'Month') {
        next = { ...next, day: null };
      }

      return next;
    });
    this.visitMutationErrorKeySignal.set(null);
  }

  saveVisitMetadata(): void {
    const visit: PassportVisit | null = this.visitSignal();
    if (!visit || !this.metadataCanSave()) {
      return;
    }

    const mapping: PassportVisitMetadataMappingResult = mapPassportVisitMetadataDraft(
      this.metadataDraftSignal(),
      visit.version);
    if (!mapping.request) {
      this.visitMutationErrorKeySignal.set(
        mapping.errorKey ?? 'passport.editor.visit.errors.save');
      return;
    }

    const request: UpdatePassportVisitRequest = mapping.request;
    const submittedFingerprint: string = this.metadataRequestFingerprint(request);
    this.runVisitMutation(
      submittedFingerprint,
      null,
      'passport.editor.visit.messages.updated',
      () => this.visitsApi.updateVisit(visit.id, request));
  }

  completeVisit(): void {
    this.changeVisitStatus('Completed', 'passport.editor.visit.messages.completed');
  }

  reopenVisit(): void {
    this.changeVisitStatus('Draft', 'passport.editor.visit.messages.reopened');
  }

  archiveVisit(): void {
    this.changeVisitStatus('Archived', 'passport.editor.visit.messages.archived');
  }

  loadDeletionPreview(): void {
    const visit: PassportVisit | null = this.visitSignal();
    if (!visit || this.deletionPreviewLoadingSignal() || this.deletionSubmittingSignal()) {
      return;
    }

    if (this.hasUnsavedStatusTransitionChanges()) {
      this.deletionErrorKeySignal.set('passport.editor.deletion.errors.saveFirst');
      return;
    }

    const visitGeneration: number = this.visitInstanceGeneration;
    this.deletionPreviewLoadingSignal.set(true);
    this.deletionErrorKeySignal.set(null);
    this.visitsApi.getDeletionPreview(visit.id).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (preview: PassportVisitDeletionPreview): void => {
        if (!this.isCurrentVisitInstance(visit.id, visitGeneration)) {
          return;
        }

        this.deletionPreviewLoadingSignal.set(false);
        this.deletionPreviewSignal.set(preview);
      },
      error: (error: unknown): void => {
        if (!this.isCurrentVisitInstance(visit.id, visitGeneration)) {
          return;
        }

        this.deletionPreviewLoadingSignal.set(false);
        this.deletionErrorKeySignal.set(this.resolveDeletionErrorKey(error));
      }
    });
  }

  cancelVisitDeletion(): void {
    if (this.deletionSubmittingSignal()) {
      return;
    }

    this.deletionPreviewSignal.set(null);
    this.deletionErrorKeySignal.set(null);
    this.pendingMutations.delete('delete-visit');
  }

  deleteVisit(): void {
    const visit: PassportVisit | null = this.visitSignal();
    const preview: PassportVisitDeletionPreview | null = this.deletionPreviewSignal();
    if (!visit
      || !preview
      || preview.visitId !== visit.id
      || this.deletionSubmittingSignal()) {
      return;
    }

    const request: DeletePassportVisitRequest = {
      expectedVersion: preview.expectedVersion,
      confirmedOccurrenceCount: preview.occurrenceCount,
      confirmedAssessmentCount: preview.assessmentCount
    };
    const operationName: string = 'delete-visit';
    const idempotencyKey: string = this.resolveIdempotencyKey(operationName, request);
    const visitGeneration: number = this.visitInstanceGeneration;
    this.deletionSubmittingSignal.set(true);
    this.deletionErrorKeySignal.set(null);
    this.visitsApi.deleteVisit(visit.id, request, idempotencyKey).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (receipt: PassportVisitDeletionReceipt): void => {
        if (!this.isCurrentVisitInstance(visit.id, visitGeneration)) {
          return;
        }

        this.deletionSubmittingSignal.set(false);
        this.pendingMutations.delete(operationName);
        this.deletedVisitIdSignal.set(receipt.visitId);
        this.showSuccess('passport.editor.deletion.deleted');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentVisitInstance(visit.id, visitGeneration)) {
          return;
        }

        this.deletionSubmittingSignal.set(false);
        const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
        if (errorCode === 'visit.deletion-preview-changed'
          || errorCode === 'visit.version-conflict') {
          this.deletionPreviewSignal.set(null);
          this.pendingMutations.delete(operationName);
        }
        this.deletionErrorKeySignal.set(this.resolveDeletionErrorKey(error));
      }
    });
  }

  private changeVisitStatus(targetStatus: PassportVisitStatus, successKey: string): void {
    const visit: PassportVisit | null = this.visitSignal();
    if (!visit || this.visitMutationSavingSignal()) {
      return;
    }

    if (this.hasUnsavedStatusTransitionChanges()) {
      this.visitMutationErrorKeySignal.set('passport.editor.visit.errors.saveBeforeStatus');
      return;
    }

    const request: Observable<PassportVisit> = targetStatus === 'Completed'
      ? this.visitsApi.completeVisit(visit.id, visit.version)
      : targetStatus === 'Draft'
        ? this.visitsApi.reopenVisit(visit.id, visit.version)
        : this.visitsApi.archiveVisit(visit.id, visit.version);
    this.runVisitMutation(
      this.metadataDraftFingerprint(this.metadataDraftSignal()),
      targetStatus,
      successKey,
      () => request);
  }

  private runVisitMutation(
    submittedFingerprint: string,
    targetStatus: PassportVisitStatus | null,
    successKey: string,
    requestFactory: () => Observable<PassportVisit>
  ): void {
    const visit: PassportVisit | null = this.visitSignal();
    if (!visit || this.visitMutationSavingSignal()) {
      return;
    }

    const visitId: string = visit.id;
    const visitGeneration: number = this.visitInstanceGeneration;
    const mutationGeneration: number = ++this.visitMutationGeneration;
    this.visitMutationSavingSignal.set(true);
    this.visitMutationErrorKeySignal.set(null);
    requestFactory().pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updatedVisit: PassportVisit): void => {
        if (!this.isCurrentVisitMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        this.visitMutationSavingSignal.set(false);
        this.applyVisitMutationResult(updatedVisit, submittedFingerprint);
        this.showSuccess(successKey);
      },
      error: (error: unknown): void => {
        if (!this.isCurrentVisitMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        if (this.isAmbiguousMutationError(error) || this.isVisitVersionConflict(error)) {
          this.reconcileVisitMutation(
            visitId,
            visitGeneration,
            mutationGeneration,
            submittedFingerprint,
            targetStatus,
            successKey,
            error);
          return;
        }

        this.visitMutationSavingSignal.set(false);
        this.visitMutationErrorKeySignal.set(this.resolveVisitMutationErrorKey(error));
      }
    });
  }

  updateParkAssessmentDraft(patch: Partial<PassportVisitParkAssessmentDraft>): void {
    this.assessmentDraftSignal.update((current: PassportVisitParkAssessmentDraft) => ({
      ...current,
      ...patch
    }));
    this.assessmentErrorKeySignal.set(null);
  }

  saveParkAssessment(): void {
    const visit: PassportVisit | null = this.visitSignal();
    const draft: PassportVisitParkAssessmentDraft = this.assessmentDraftSignal();
    if (!visit || draft.value === null || !this.assessmentCanSave()) {
      return;
    }

    const visitId: string = visit.id;
    const visitGeneration: number = this.visitInstanceGeneration;
    const mutationGeneration: number = ++this.assessmentMutationGeneration;
    const request: UpsertPassportVisitParkAssessmentRequest = {
      value: draft.value,
      privateComment: draft.privateComment.trim() || null,
      expectedVersion: visit.version
    };
    const submittedFingerprint: string = this.assessmentDraftFingerprint({
      value: request.value,
      privateComment: request.privateComment ?? ''
    });
    this.assessmentSavingSignal.set(true);
    this.assessmentErrorKeySignal.set(null);
    this.visitsApi.upsertParkAssessment(visitId, request).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (updatedVisit: PassportVisit): void => {
        if (!this.isCurrentAssessmentMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        this.assessmentSavingSignal.set(false);
        this.applyAssessmentMutationResult(updatedVisit, submittedFingerprint);
        this.showSuccess('passport.editor.assessment.saved');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentAssessmentMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        if (this.isAmbiguousMutationError(error) || this.isAssessmentVersionConflict(error)) {
          this.reconcileAssessmentMutation(
            visitId,
            visitGeneration,
            mutationGeneration,
            submittedFingerprint,
            'upsert',
            error);
          return;
        }

        this.assessmentSavingSignal.set(false);
        this.assessmentErrorKeySignal.set(this.resolveAssessmentErrorKey(error));
      }
    });
  }

  deleteParkAssessment(): void {
    const visit: PassportVisit | null = this.visitSignal();
    if (!visit?.parkAssessment || this.assessmentSavingSignal()) {
      return;
    }

    const visitId: string = visit.id;
    const visitGeneration: number = this.visitInstanceGeneration;
    const mutationGeneration: number = ++this.assessmentMutationGeneration;
    const submittedFingerprint: string = this.assessmentDraftFingerprint(this.assessmentDraftSignal());
    this.assessmentSavingSignal.set(true);
    this.assessmentErrorKeySignal.set(null);
    this.visitsApi.deleteParkAssessment(visitId, visit.version).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (updatedVisit: PassportVisit): void => {
        if (!this.isCurrentAssessmentMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        this.assessmentSavingSignal.set(false);
        this.applyAssessmentMutationResult(updatedVisit, submittedFingerprint);
        this.showSuccess('passport.editor.assessment.deleted');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentAssessmentMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        if (this.isAmbiguousMutationError(error) || this.isAssessmentVersionConflict(error)) {
          this.reconcileAssessmentMutation(
            visitId,
            visitGeneration,
            mutationGeneration,
            submittedFingerprint,
            'delete',
            error);
          return;
        }

        this.assessmentSavingSignal.set(false);
        this.assessmentErrorKeySignal.set(this.resolveAssessmentErrorKey(error));
      }
    });
  }

  updateRideAssessmentDraft(occurrenceId: string, patch: Partial<PassportRideAssessmentDraft>): void {
    this.rideAssessmentDraftsSignal.update(
      (current: Readonly<Record<string, PassportRideAssessmentDraft>>) => ({
        ...current,
        [occurrenceId]: {
          ...(current[occurrenceId] ?? { value: null, privateComment: '' }),
          ...patch
        }
      })
    );
    this.setRideAssessmentError(occurrenceId, null);
  }

  rideAssessmentHasChanges(occurrenceId: string): boolean {
    const draft: PassportRideAssessmentDraft | undefined = this.rideAssessmentDraftsSignal()[occurrenceId];
    if (!draft) {
      return false;
    }

    return this.rideAssessmentDraftFingerprint(draft)
      !== this.persistedRideAssessmentFingerprints.get(occurrenceId);
  }

  canSaveRideAssessment(occurrence: PassportRideOccurrence): boolean {
    const draft: PassportRideAssessmentDraft | undefined = this.rideAssessmentDraftsSignal()[occurrence.id];
    return draft?.value != null
      && !this.isOccurrenceBusy(occurrence.id)
      && this.rideAssessmentHasChanges(occurrence.id);
  }

  saveRideAssessment(occurrence: PassportRideOccurrence): void {
    const visitId: string | null = this.currentVisitId;
    const draft: PassportRideAssessmentDraft | undefined = this.rideAssessmentDraftsSignal()[occurrence.id];
    if (!visitId || !draft || draft.value === null || !this.canSaveRideAssessment(occurrence)) {
      return;
    }

    const request: UpsertPassportRideAssessmentRequest = {
      value: draft.value,
      privateComment: draft.privateComment.trim() || null,
      expectedVersion: occurrence.version
    };
    const submittedFingerprint: string = this.rideAssessmentDraftFingerprint(draft);
    const visitGeneration: number = this.visitInstanceGeneration;
    const mutationGeneration: number = this.nextRideAssessmentMutationGeneration(occurrence.id);
    this.setOccurrenceBusy(occurrence.id, true);
    this.setRideAssessmentError(occurrence.id, null);
    this.occurrencesApi.upsertAssessment(occurrence.id, request).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (updated: PassportRideOccurrence): void => {
        if (!this.isCurrentRideAssessmentMutation(
          visitId,
          visitGeneration,
          occurrence.id,
          mutationGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.applyRideAssessmentMutationResult(updated, submittedFingerprint);
        this.showSuccess('passport.editor.rideAssessment.saved');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentRideAssessmentMutation(
          visitId,
          visitGeneration,
          occurrence.id,
          mutationGeneration)) {
          return;
        }

        if (this.isAmbiguousMutationError(error) || this.isRideAssessmentVersionConflict(error)) {
          this.reconcileRideAssessmentMutation(
            visitId,
            visitGeneration,
            occurrence.id,
            mutationGeneration,
            submittedFingerprint,
            'upsert',
            error);
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.setRideAssessmentError(occurrence.id, this.resolveRideAssessmentErrorKey(error));
      }
    });
  }

  deleteRideAssessment(occurrence: PassportRideOccurrence): void {
    const visitId: string | null = this.currentVisitId;
    if (!visitId || !occurrence.assessment || this.isOccurrenceBusy(occurrence.id)) {
      return;
    }

    const submittedFingerprint: string = this.rideAssessmentDraftFingerprint(
      this.rideAssessmentDraftsSignal()[occurrence.id] ?? { value: null, privateComment: '' });
    const visitGeneration: number = this.visitInstanceGeneration;
    const mutationGeneration: number = this.nextRideAssessmentMutationGeneration(occurrence.id);
    this.setOccurrenceBusy(occurrence.id, true);
    this.setRideAssessmentError(occurrence.id, null);
    this.occurrencesApi.deleteAssessment(occurrence.id, occurrence.version).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (updated: PassportRideOccurrence): void => {
        if (!this.isCurrentRideAssessmentMutation(
          visitId,
          visitGeneration,
          occurrence.id,
          mutationGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.applyRideAssessmentMutationResult(updated, submittedFingerprint);
        this.showSuccess('passport.editor.rideAssessment.deleted');
      },
      error: (error: unknown): void => {
        if (!this.isCurrentRideAssessmentMutation(
          visitId,
          visitGeneration,
          occurrence.id,
          mutationGeneration)) {
          return;
        }

        if (this.isAmbiguousMutationError(error) || this.isRideAssessmentVersionConflict(error)) {
          this.reconcileRideAssessmentMutation(
            visitId,
            visitGeneration,
            occurrence.id,
            mutationGeneration,
            submittedFingerprint,
            'delete',
            error);
          return;
        }

        this.setOccurrenceBusy(occurrence.id, false);
        this.setRideAssessmentError(occurrence.id, this.resolveRideAssessmentErrorKey(error));
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
      confirmHistoricalConflict: this.editDraftsSignal()[occurrence.id]?.confirmHistoricalConflict
        ?? occurrence.historicalConflictConfirmed
        ?? false,
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
    const resolvesStaleConsistency: boolean = this.timelineConsistencyStaleSignal();
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
          this.timelineConsistencyStaleSignal.set(false);
          if (resolvesStaleConsistency) {
            this.operationErrorKeySignal.set(null);
          }
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
      attractions: this.loadEvaluatedAttractionPage(
        visit.id,
        parkId,
        attractionPage
      ).pipe(catchError(() => of<EvaluatedAttractionPage | null>(null))),
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

        this.applyLoadedVisit(visit);
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
    const visit: PassportVisit | null = this.visitSignal();
    if (!visit || this.attractionsLoadingSignal()) {
      return;
    }

    const attractionGeneration: number = ++this.attractionLoadGeneration;
    this.attractionsLoadingSignal.set(true);
    this.attractionErrorKeySignal.set(null);
    this.loadEvaluatedAttractionPage(visit.id, visit.parkId, page)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: EvaluatedAttractionPage): void => {
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

  private loadEvaluatedAttractionPage(
    visitId: string,
    parkId: string,
    page: number
  ): Observable<EvaluatedAttractionPage> {
    return this.attractionsApi.getParkItemsByParkIdPage(
      parkId,
      page,
      PassportVisitEditorStateFacade.AttractionPageSize,
      {
        includeHidden: false,
        closedFilter: 'all',
        category: 'Attraction',
        search: this.currentAttractionSearch || null,
        zoneId: this.currentZoneId
      },
      { closedFilter: 'all' }
    ).pipe(switchMap((result: PagedResult<ParkItem>): Observable<EvaluatedAttractionPage> => {
      const visibleItems: ParkItem[] = result.items.filter(
        (item: ParkItem): boolean => item.isVisible !== false
      );
      const parkItemIds: string[] = visibleItems
        .map((item: ParkItem): string => item.id?.trim() ?? '')
        .filter((id: string): boolean => id.length > 0);
      if (parkItemIds.length === 0) {
        return of({ page: { ...result, items: visibleItems }, evaluations: [] });
      }

      return this.evaluateVisitTargetsInBatches(visitId, parkItemIds).pipe(
        map((evaluations: PassportVisitRideTargetEvaluation[]): EvaluatedAttractionPage => ({
          page: { ...result, items: visibleItems },
          evaluations
        }))
      );
    }));
  }

  private evaluateVisitTargetsInBatches(
    visitId: string,
    parkItemIds: readonly string[]
  ): Observable<PassportVisitRideTargetEvaluation[]> {
    const batches: string[][] = [];
    for (let index: number = 0; index < parkItemIds.length; index += PassportVisitEditorStateFacade.TargetEvaluationBatchSize) {
      batches.push(parkItemIds.slice(index, index + PassportVisitEditorStateFacade.TargetEvaluationBatchSize));
    }
    if (batches.length === 0) {
      return of([]);
    }

    return forkJoin(batches.map((batch: string[]): Observable<PassportVisitRideTargetEvaluation[]> =>
      this.occurrencesApi.evaluateVisitTargets(visitId, batch)
    )).pipe(map((results: PassportVisitRideTargetEvaluation[][]): PassportVisitRideTargetEvaluation[] =>
      results.flat()
    ));
  }

  private applyAttractionPage(result: EvaluatedAttractionPage): void {
    const evaluationsById: ReadonlyMap<string, PassportVisitRideTargetEvaluation> = new Map(
      result.evaluations.map(
        (evaluation: PassportVisitRideTargetEvaluation): [string, PassportVisitRideTargetEvaluation] =>
          [evaluation.parkItemId, evaluation]
      )
    );
    const attractions: PassportVisitEditorAttraction[] = result.page.items
      .map((item: ParkItem): PassportVisitEditorAttraction | null =>
        mapParkItemToVisitEditorAttraction(
          item,
          item.id ? evaluationsById.get(item.id) ?? null : null
        ))
      .filter((item: PassportVisitEditorAttraction | null): item is PassportVisitEditorAttraction => item !== null);
    this.attractionsSignal.set(attractions);
    this.attractionPaginationSignal.set(result.page.pagination);
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
    const currentOccurrences: Map<string, PassportRideOccurrence> = new Map<string, PassportRideOccurrence>(
      this.occurrencesSignal().map((occurrence: PassportRideOccurrence) => [occurrence.id, occurrence])
    );
    const effectiveOccurrences: PassportRideOccurrence[] = occurrences.map(
      (occurrence: PassportRideOccurrence): PassportRideOccurrence => {
        const current: PassportRideOccurrence | undefined = currentOccurrences.get(occurrence.id);
        return current && current.version > occurrence.version ? current : occurrence;
      }
    );
    const currentDrafts: Readonly<Record<string, PassportOccurrenceEditDraft>> = this.editDraftsSignal();
    const currentAssessmentDrafts: Readonly<Record<string, PassportRideAssessmentDraft>> =
      this.rideAssessmentDraftsSignal();
    const nextDrafts: Record<string, PassportOccurrenceEditDraft> = { ...currentDrafts };
    const nextAssessmentDrafts: Record<string, PassportRideAssessmentDraft> = { ...currentAssessmentDrafts };
    const nextFingerprints: Map<string, string> = new Map<string, string>(this.persistedEditFingerprints);
    const nextAssessmentFingerprints: Map<string, string> =
      new Map<string, string>(this.persistedRideAssessmentFingerprints);

    for (const occurrence of effectiveOccurrences) {
      const persistedDraft: PassportOccurrenceEditDraft = mapOccurrenceToEditDraft(occurrence);
      const persistedFingerprint: string = JSON.stringify(persistedDraft);
      const currentDraft: PassportOccurrenceEditDraft | undefined = currentDrafts[occurrence.id];
      nextDrafts[occurrence.id] = currentDraft
        && this.persistedEditFingerprints.get(occurrence.id) === persistedFingerprint
          ? currentDraft
          : persistedDraft;
      nextFingerprints.set(occurrence.id, persistedFingerprint);

      const persistedAssessmentDraft: PassportRideAssessmentDraft = this.mapRideAssessmentToDraft(
        occurrence.assessment ?? null);
      const persistedAssessmentFingerprint: string =
        this.rideAssessmentDraftFingerprint(persistedAssessmentDraft);
      const currentAssessmentDraft: PassportRideAssessmentDraft | undefined =
        currentAssessmentDrafts[occurrence.id];
      nextAssessmentDrafts[occurrence.id] = currentAssessmentDraft
        && this.persistedRideAssessmentFingerprints.get(occurrence.id) === persistedAssessmentFingerprint
          ? currentAssessmentDraft
          : persistedAssessmentDraft;
      nextAssessmentFingerprints.set(occurrence.id, persistedAssessmentFingerprint);
    }

    this.persistedEditFingerprints.clear();
    for (const [occurrenceId, fingerprint] of nextFingerprints) {
      this.persistedEditFingerprints.set(occurrenceId, fingerprint);
    }
    this.persistedRideAssessmentFingerprints.clear();
    for (const [occurrenceId, fingerprint] of nextAssessmentFingerprints) {
      this.persistedRideAssessmentFingerprints.set(occurrenceId, fingerprint);
    }

    this.editDraftsSignal.set(nextDrafts);
    this.rideAssessmentDraftsSignal.set(nextAssessmentDrafts);
    this.occurrencesSignal.set(effectiveOccurrences);
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
    this.persistedRideAssessmentFingerprints.delete(occurrenceId);
    this.editDraftsSignal.update((current: Readonly<Record<string, PassportOccurrenceEditDraft>>) => {
      const next: Record<string, PassportOccurrenceEditDraft> = { ...current };
      delete next[occurrenceId];
      return next;
    });
    this.rideAssessmentDraftsSignal.update(
      (current: Readonly<Record<string, PassportRideAssessmentDraft>>) => {
        const next: Record<string, PassportRideAssessmentDraft> = { ...current };
        delete next[occurrenceId];
        return next;
      }
    );
    this.rideAssessmentErrorKeysSignal.update((current: Readonly<Record<string, string | null>>) => {
      const next: Record<string, string | null> = { ...current };
      delete next[occurrenceId];
      return next;
    });
    this.rideAssessmentMutationGenerations.delete(occurrenceId);
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

  private resolveDeletionErrorKey(error: unknown): string {
    const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
    if (errorCode === 'visit.deletion-preview-changed' || errorCode === 'visit.version-conflict') {
      return 'passport.editor.deletion.errors.changed';
    }

    if (errorCode === 'visit.not-found') {
      return 'passport.editor.deletion.errors.notFound';
    }

    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'passport.editor.deletion.errors.network';
    }

    return 'passport.editor.deletion.errors.generic';
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

  private reconcileRideAssessmentMutation(
    visitId: string,
    visitGeneration: number,
    occurrenceId: string,
    mutationGeneration: number,
    submittedFingerprint: string,
    mutation: 'upsert' | 'delete',
    originalError: unknown
  ): void {
    this.occurrencesApi.get(visitId, occurrenceId).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (currentOccurrence: PassportRideOccurrence): void => {
        if (!this.isCurrentRideAssessmentMutation(
          visitId,
          visitGeneration,
          occurrenceId,
          mutationGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrenceId, false);
        const serverFingerprint: string = this.rideAssessmentFingerprint(currentOccurrence.assessment ?? null);
        const mutationWasApplied: boolean = mutation === 'delete'
          ? currentOccurrence.assessment == null
          : serverFingerprint === submittedFingerprint;
        const currentDraft: PassportRideAssessmentDraft | undefined =
          this.rideAssessmentDraftsSignal()[occurrenceId];
        const draftChangedDuringRequest: boolean = currentDraft != null
          && this.rideAssessmentDraftFingerprint(currentDraft) !== submittedFingerprint;
        this.replaceOccurrence(currentOccurrence);
        if (!mutationWasApplied || draftChangedDuringRequest) {
          this.restoreRideAssessmentDraft(occurrenceId, currentDraft);
        }
        if (mutationWasApplied && this.isAmbiguousMutationError(originalError)) {
          this.setRideAssessmentError(occurrenceId, null);
          this.showSuccess(mutation === 'delete'
            ? 'passport.editor.rideAssessment.deleted'
            : 'passport.editor.rideAssessment.saved');
          return;
        }

        this.setRideAssessmentError(occurrenceId, 'passport.editor.rideAssessment.errors.conflict');
      },
      error: (): void => {
        if (!this.isCurrentRideAssessmentMutation(
          visitId,
          visitGeneration,
          occurrenceId,
          mutationGeneration)) {
          return;
        }

        this.setOccurrenceBusy(occurrenceId, false);
        this.setRideAssessmentError(occurrenceId, 'passport.editor.rideAssessment.errors.recovery');
      }
    });
  }

  private applyRideAssessmentMutationResult(
    occurrence: PassportRideOccurrence,
    submittedFingerprint: string
  ): void {
    const currentDraft: PassportRideAssessmentDraft | undefined =
      this.rideAssessmentDraftsSignal()[occurrence.id];
    const draftChangedDuringRequest: boolean = currentDraft != null
      && this.rideAssessmentDraftFingerprint(currentDraft) !== submittedFingerprint;
    this.replaceOccurrence(occurrence);
    if (draftChangedDuringRequest) {
      this.restoreRideAssessmentDraft(occurrence.id, currentDraft);
    }
    this.setRideAssessmentError(occurrence.id, null);
  }

  private replaceOccurrence(updated: PassportRideOccurrence): void {
    const nextOccurrences: PassportRideOccurrence[] = this.occurrencesSignal().map(
      (candidate: PassportRideOccurrence): PassportRideOccurrence => candidate.id === updated.id
        ? {
          ...updated,
          target: updated.target ?? candidate.target,
          historicalConsistency: updated.target == null
            ? candidate.historicalConsistency
            : updated.historicalConsistency,
          historicalConflictConfirmed: updated.target == null
            ? candidate.historicalConflictConfirmed
            : updated.historicalConflictConfirmed
        }
        : candidate
    );
    this.setOccurrences(nextOccurrences);
  }

  private restoreRideAssessmentDraft(
    occurrenceId: string,
    draft: PassportRideAssessmentDraft | undefined
  ): void {
    if (!draft) {
      return;
    }

    this.rideAssessmentDraftsSignal.update(
      (current: Readonly<Record<string, PassportRideAssessmentDraft>>) => ({
        ...current,
        [occurrenceId]: draft
      })
    );
  }

  private mapRideAssessmentToDraft(assessment: PassportRideAssessment | null): PassportRideAssessmentDraft {
    return {
      value: assessment?.value ?? null,
      privateComment: assessment?.privateComment ?? ''
    };
  }

  private rideAssessmentFingerprint(assessment: PassportRideAssessment | null): string {
    return this.rideAssessmentDraftFingerprint(this.mapRideAssessmentToDraft(assessment));
  }

  private rideAssessmentDraftFingerprint(draft: PassportRideAssessmentDraft): string {
    return JSON.stringify({
      value: draft.value,
      privateComment: draft.privateComment.trim() || null
    });
  }

  private nextRideAssessmentMutationGeneration(occurrenceId: string): number {
    const nextGeneration: number = (this.rideAssessmentMutationGenerations.get(occurrenceId) ?? 0) + 1;
    this.rideAssessmentMutationGenerations.set(occurrenceId, nextGeneration);
    return nextGeneration;
  }

  private isCurrentRideAssessmentMutation(
    visitId: string,
    visitGeneration: number,
    occurrenceId: string,
    mutationGeneration: number
  ): boolean {
    return this.isCurrentVisitInstance(visitId, visitGeneration)
      && this.rideAssessmentMutationGenerations.get(occurrenceId) === mutationGeneration;
  }

  private isRideAssessmentVersionConflict(error: unknown): boolean {
    return extractApiProblemDetails(error)?.errorCode === 'ride-assessment.version-conflict';
  }

  private resolveRideAssessmentErrorKey(error: unknown): string {
    const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
    if (errorCode === 'rating.invalid-value' || errorCode === 'rating.invalid-step') {
      return 'passport.editor.rideAssessment.errors.invalidValue';
    }

    if (errorCode === 'ride-assessment.private-comment-too-long') {
      return 'passport.editor.rideAssessment.errors.commentTooLong';
    }

    if (errorCode === 'ride-occurrence.not-found') {
      return 'passport.editor.rideAssessment.errors.occurrenceNotFound';
    }

    if (this.isRideAssessmentVersionConflict(error)) {
      return 'passport.editor.rideAssessment.errors.conflict';
    }

    return this.isAmbiguousMutationError(error)
      ? 'passport.editor.rideAssessment.errors.recovery'
      : 'passport.editor.rideAssessment.errors.save';
  }

  private setRideAssessmentError(occurrenceId: string, errorKey: string | null): void {
    this.rideAssessmentErrorKeysSignal.update((current: Readonly<Record<string, string | null>>) => ({
      ...current,
      [occurrenceId]: errorKey
    }));
  }

  private reconcileAssessmentMutation(
    visitId: string,
    visitGeneration: number,
    mutationGeneration: number,
    submittedFingerprint: string,
    mutation: 'upsert' | 'delete',
    originalError: unknown
  ): void {
    this.visitsApi.getVisit(visitId).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (currentVisit: PassportVisit): void => {
        if (!this.isCurrentAssessmentMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        this.assessmentSavingSignal.set(false);
        const serverFingerprint: string = this.assessmentFingerprint(currentVisit.parkAssessment ?? null);
        const mutationWasApplied: boolean = mutation === 'delete'
          ? currentVisit.parkAssessment == null
          : serverFingerprint === submittedFingerprint;
        const draftChangedDuringRequest: boolean =
          this.assessmentDraftFingerprint(this.assessmentDraftSignal()) !== submittedFingerprint;
        const preserveMetadataDraft: boolean = this.metadataHasChanges();
        const temporalMetadataChanged: boolean = this.hasTemporalMetadataChanged(
          this.visitSignal(),
          currentVisit);
        this.visitSignal.set(currentVisit);
        this.persistedMetadataFingerprintSignal.set(this.metadataVisitFingerprint(currentVisit));
        this.persistedAssessmentFingerprintSignal.set(serverFingerprint);
        if (!preserveMetadataDraft) {
          this.metadataDraftSignal.set(createPassportVisitMetadataDraft(currentVisit));
        }
        if (mutationWasApplied && !draftChangedDuringRequest) {
          this.syncAssessmentDraft(currentVisit.parkAssessment ?? null);
        }
        if (temporalMetadataChanged) {
          this.refreshHistoricalEvidence(currentVisit.id);
        }
        if (mutationWasApplied && this.isAmbiguousMutationError(originalError)) {
          this.assessmentErrorKeySignal.set(null);
          this.showSuccess(mutation === 'delete'
            ? 'passport.editor.assessment.deleted'
            : 'passport.editor.assessment.saved');
          return;
        }

        this.assessmentErrorKeySignal.set('passport.editor.assessment.errors.conflict');
      },
      error: (): void => {
        if (!this.isCurrentAssessmentMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        this.assessmentSavingSignal.set(false);
        this.assessmentErrorKeySignal.set('passport.editor.assessment.errors.recovery');
      }
    });
  }

  private applyLoadedVisit(visit: PassportVisit): void {
    const currentVisit: PassportVisit | null = this.visitSignal();
    if (currentVisit?.id === visit.id && visit.version < currentVisit.version) {
      return;
    }

    const temporalMetadataChanged: boolean = this.hasTemporalMetadataChanged(currentVisit, visit);
    const preserveAssessmentDraft: boolean = currentVisit?.id === visit.id && this.assessmentHasChanges();
    const preserveMetadataDraft: boolean = currentVisit?.id === visit.id && this.metadataHasChanges();
    this.visitSignal.set(visit);
    this.persistedMetadataFingerprintSignal.set(this.metadataVisitFingerprint(visit));
    this.persistedAssessmentFingerprintSignal.set(this.assessmentFingerprint(visit.parkAssessment ?? null));
    if (!preserveMetadataDraft) {
      this.metadataDraftSignal.set(createPassportVisitMetadataDraft(visit));
    }
    if (!preserveAssessmentDraft) {
      this.syncAssessmentDraft(visit.parkAssessment ?? null);
    }
    if (temporalMetadataChanged) {
      this.refreshHistoricalEvidence(visit.id);
    }
  }

  private applyVisitMutationResult(visit: PassportVisit, submittedFingerprint: string): void {
    const previousVisit: PassportVisit | null = this.visitSignal();
    const temporalMetadataChanged: boolean = this.hasTemporalMetadataChanged(previousVisit, visit);
    const draftChangedDuringRequest: boolean =
      this.metadataDraftFingerprint(this.metadataDraftSignal()) !== submittedFingerprint;
    this.visitSignal.set(visit);
    this.persistedMetadataFingerprintSignal.set(this.metadataVisitFingerprint(visit));
    this.persistedAssessmentFingerprintSignal.set(this.assessmentFingerprint(visit.parkAssessment ?? null));
    if (!draftChangedDuringRequest) {
      this.metadataDraftSignal.set(createPassportVisitMetadataDraft(visit));
    }
    if (temporalMetadataChanged) {
      this.refreshHistoricalEvidence(visit.id);
    }
  }

  private hasTemporalMetadataChanged(previousVisit: PassportVisit | null, currentVisit: PassportVisit): boolean {
    return previousVisit !== null
      && this.temporalMetadataDraftFingerprint(createPassportVisitMetadataDraft(previousVisit))
        !== this.temporalMetadataDraftFingerprint(createPassportVisitMetadataDraft(currentVisit));
  }

  private refreshHistoricalEvidence(visitId: string): void {
    this.timelineConsistencyStaleSignal.set(true);
    this.refreshLoadedTargetEvaluations(visitId);
    this.reloadTimeline();
  }

  private refreshLoadedTargetEvaluations(visitId: string): void {
    const parkItemIds: string[] = Array.from(new Set<string>([
      ...this.attractionsSignal().map((attraction: PassportVisitEditorAttraction): string => attraction.id),
      ...this.selectedAttractionsSignal().map(
        (selection: PassportAttractionSelectionDraft): string => selection.parkItemId
      )
    ]));
    if (parkItemIds.length === 0) {
      this.targetEvaluationsStaleSignal.set(false);
      return;
    }

    const attractionGeneration: number = ++this.attractionLoadGeneration;
    this.targetEvaluationsStaleSignal.set(true);
    this.attractionsLoadingSignal.set(true);
    this.attractionErrorKeySignal.set(null);
    this.evaluateVisitTargetsInBatches(visitId, parkItemIds).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (evaluations: PassportVisitRideTargetEvaluation[]): void => {
        if (attractionGeneration !== this.attractionLoadGeneration) {
          return;
        }

        this.attractionsLoadingSignal.set(false);
        this.applyTargetEvaluations(evaluations);
        this.targetEvaluationsStaleSignal.set(false);
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

  private applyTargetEvaluations(evaluations: PassportVisitRideTargetEvaluation[]): void {
    const evaluationsById: ReadonlyMap<string, PassportVisitRideTargetEvaluation> = new Map(
      evaluations.map(
        (evaluation: PassportVisitRideTargetEvaluation): [string, PassportVisitRideTargetEvaluation] =>
          [evaluation.parkItemId, evaluation]
      )
    );
    this.attractionsSignal.update((attractions: PassportVisitEditorAttraction[]) => attractions.map(
      (attraction: PassportVisitEditorAttraction): PassportVisitEditorAttraction => {
        const evaluation: PassportVisitRideTargetEvaluation | undefined = evaluationsById.get(attraction.id);
        return evaluation ? {
          ...attraction,
          historicalConsistency: evaluation.historicalConsistency,
          openingDate: evaluation.openingDate,
          closingDate: evaluation.closingDate
        } : attraction;
      }
    ));
    this.selectedAttractionsSignal.update((selections: PassportAttractionSelectionDraft[]) => selections.map(
      (selection: PassportAttractionSelectionDraft): PassportAttractionSelectionDraft => {
        const evaluation: PassportVisitRideTargetEvaluation | undefined =
          evaluationsById.get(selection.parkItemId);
        return evaluation ? {
          ...selection,
          historicalConsistency: evaluation.historicalConsistency,
          openingDate: evaluation.openingDate,
          closingDate: evaluation.closingDate,
          confirmHistoricalConflict: false
        } : selection;
      }
    ));
  }

  private reconcileVisitMutation(
    visitId: string,
    visitGeneration: number,
    mutationGeneration: number,
    submittedFingerprint: string,
    targetStatus: PassportVisitStatus | null,
    successKey: string,
    originalError: unknown
  ): void {
    this.visitsApi.getVisit(visitId).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (currentVisit: PassportVisit): void => {
        if (!this.isCurrentVisitMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        this.visitMutationSavingSignal.set(false);
        const mutationWasApplied: boolean = targetStatus
          ? currentVisit.status === targetStatus
          : this.metadataVisitFingerprint(currentVisit) === submittedFingerprint;
        if (mutationWasApplied && this.isAmbiguousMutationError(originalError)) {
          this.applyVisitMutationResult(currentVisit, submittedFingerprint);
          this.showSuccess(successKey);
          return;
        }

        this.applyLoadedVisit(currentVisit);
        this.visitMutationErrorKeySignal.set('passport.editor.visit.errors.conflict');
      },
      error: (): void => {
        if (!this.isCurrentVisitMutation(visitId, visitGeneration, mutationGeneration)) {
          return;
        }

        this.visitMutationSavingSignal.set(false);
        this.visitMutationErrorKeySignal.set('passport.editor.visit.errors.recovery');
      }
    });
  }

  private metadataVisitFingerprint(visit: PassportVisit): string {
    return this.metadataDraftFingerprint(createPassportVisitMetadataDraft(visit));
  }

  private metadataDraftFingerprint(draft: PassportVisitMetadataDraft): string {
    const mapping: PassportVisitMetadataMappingResult = mapPassportVisitMetadataDraft(draft, 1);
    return mapping.request
      ? this.metadataRequestFingerprint(mapping.request)
      : JSON.stringify(draft);
  }

  private temporalMetadataDraftFingerprint(draft: PassportVisitMetadataDraft): string {
    const mapping: PassportVisitMetadataMappingResult = mapPassportVisitMetadataDraft(
      { ...draft, title: '', privateNote: '' },
      1);
    if (mapping.request) {
      return JSON.stringify({
        year: mapping.request.date.year,
        month: mapping.request.date.month,
        day: mapping.request.date.day,
        precision: mapping.request.date.precision,
        timeZoneId: mapping.request.timeZoneId,
        serviceDayConvention: mapping.request.serviceDayConvention
      });
    }

    return JSON.stringify({
      year: draft.year,
      month: draft.month,
      day: draft.day,
      precision: draft.precision,
      timeZoneId: draft.timeZoneId.trim(),
      serviceDayConvention: draft.serviceDayConvention
    });
  }

  private metadataRequestFingerprint(request: UpdatePassportVisitRequest): string {
    return JSON.stringify({
      date: request.date,
      timeZoneId: request.timeZoneId,
      serviceDayConvention: request.serviceDayConvention,
      title: request.title,
      privateNote: request.privateNote
    });
  }

  private isCurrentVisitMutation(
    visitId: string,
    visitGeneration: number,
    mutationGeneration: number
  ): boolean {
    return this.isCurrentVisitInstance(visitId, visitGeneration)
      && mutationGeneration === this.visitMutationGeneration;
  }

  private isVisitVersionConflict(error: unknown): boolean {
    return extractApiProblemDetails(error)?.errorCode === 'visit.version-conflict';
  }

  private resolveVisitMutationErrorKey(error: unknown): string {
    const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
    if (errorCode === 'visit.time-zone-id-invalid'
      || errorCode === 'visit.time-zone-id-too-long'
      || errorCode === 'visit.time-zone-id-control-character') {
      return 'passport.editor.visit.validation.timeZoneInvalid';
    }

    if (errorCode === 'visit.title-too-long' || errorCode === 'visit.title-control-character') {
      return 'passport.editor.visit.validation.titleTooLong';
    }

    if (errorCode === 'visit.private-note-too-long') {
      return 'passport.editor.visit.validation.noteTooLong';
    }

    if (errorCode?.startsWith('visit-date.')) {
      return 'passport.editor.visit.validation.dayInvalid';
    }

    if (errorCode === 'visit.invalid-transition'
      || errorCode === 'visit.future-completed-date'
      || errorCode === 'visit.not-editable') {
      return 'passport.editor.visit.errors.transition';
    }

    if (errorCode === 'visit.temporal-metadata-locked') {
      return 'passport.editor.visit.errors.temporalMetadataLocked';
    }

    if (errorCode === 'visit.not-found') {
      return 'passport.editor.errors.visitNotFound';
    }

    if (this.isVisitVersionConflict(error)) {
      return 'passport.editor.visit.errors.conflict';
    }

    return this.isAmbiguousMutationError(error)
      ? 'passport.editor.visit.errors.recovery'
      : 'passport.editor.visit.errors.save';
  }

  private applyAssessmentMutationResult(visit: PassportVisit, submittedFingerprint: string): void {
    const draftChangedDuringRequest: boolean =
      this.assessmentDraftFingerprint(this.assessmentDraftSignal()) !== submittedFingerprint;
    this.visitSignal.set(visit);
    this.persistedAssessmentFingerprintSignal.set(this.assessmentFingerprint(visit.parkAssessment ?? null));
    if (!draftChangedDuringRequest) {
      this.syncAssessmentDraft(visit.parkAssessment ?? null);
    }
  }

  private syncAssessmentDraft(assessment: PassportVisitParkAssessment | null): void {
    this.assessmentDraftSignal.set({
      value: assessment?.value ?? null,
      privateComment: assessment?.privateComment ?? ''
    });
  }

  private assessmentFingerprint(assessment: PassportVisitParkAssessment | null): string {
    return this.assessmentDraftFingerprint({
      value: assessment?.value ?? null,
      privateComment: assessment?.privateComment ?? ''
    });
  }

  private assessmentDraftFingerprint(draft: PassportVisitParkAssessmentDraft): string {
    return JSON.stringify({
      value: draft.value,
      privateComment: draft.privateComment.trim() || null
    });
  }

  private isCurrentAssessmentMutation(
    visitId: string,
    visitGeneration: number,
    mutationGeneration: number
  ): boolean {
    return this.isCurrentVisitInstance(visitId, visitGeneration)
      && mutationGeneration === this.assessmentMutationGeneration;
  }

  private isAssessmentVersionConflict(error: unknown): boolean {
    return extractApiProblemDetails(error)?.errorCode === 'visit-park-assessment.version-conflict';
  }

  private resolveAssessmentErrorKey(error: unknown): string {
    const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
    if (errorCode === 'rating.invalid-value' || errorCode === 'rating.invalid-step') {
      return 'passport.editor.assessment.errors.invalidValue';
    }

    if (errorCode === 'visit-park-assessment.private-comment-too-long') {
      return 'passport.editor.assessment.errors.commentTooLong';
    }

    if (errorCode === 'visit.not-found') {
      return 'passport.editor.errors.visitNotFound';
    }

    if (this.isAssessmentVersionConflict(error)) {
      return 'passport.editor.assessment.errors.conflict';
    }

    return this.isAmbiguousMutationError(error)
      ? 'passport.editor.assessment.errors.recovery'
      : 'passport.editor.assessment.errors.save';
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
    this.metadataDraftSignal.set({
      precision: 'Day',
      year: null,
      month: null,
      day: null,
      isApproximate: false,
      timeZoneId: '',
      serviceDayConvention: 'VisitStartLocalDate',
      title: '',
      privateNote: ''
    });
    this.persistedMetadataFingerprintSignal.set('');
    this.visitMutationSavingSignal.set(false);
    this.visitMutationErrorKeySignal.set(null);
    this.deletionPreviewSignal.set(null);
    this.deletionPreviewLoadingSignal.set(false);
    this.deletionSubmittingSignal.set(false);
    this.deletionErrorKeySignal.set(null);
    this.deletedVisitIdSignal.set(null);
    this.assessmentDraftSignal.set({ value: null, privateComment: '' });
    this.persistedAssessmentFingerprintSignal.set(this.assessmentFingerprint(null));
    this.assessmentSavingSignal.set(false);
    this.assessmentErrorKeySignal.set(null);
    this.parkNameSignal.set('');
    this.zonesSignal.set([]);
    this.attractionsSignal.set([]);
    this.selectedAttractionsSignal.set([]);
    this.persistedEditFingerprints.clear();
    this.persistedRideAssessmentFingerprints.clear();
    this.rideAssessmentMutationGenerations.clear();
    this.editDraftsSignal.set({});
    this.rideAssessmentDraftsSignal.set({});
    this.rideAssessmentErrorKeysSignal.set({});
    this.occurrencesSignal.set([]);
    this.nextTimelineCursorSignal.set(null);
    this.attractionNamesSignal.set({});
    this.loadErrorKeySignal.set(null);
    this.attractionErrorKeySignal.set(null);
    this.targetEvaluationsStaleSignal.set(false);
    this.timelineConsistencyStaleSignal.set(false);
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
    this.assessmentMutationGeneration += 1;
    this.visitMutationGeneration += 1;
  }
}
