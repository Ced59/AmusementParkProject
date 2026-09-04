import { Inject, Injectable, Signal, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  CreatePassportRideOccurrenceBatchItem,
  PassportRideOccurrence,
  PassportRideOccurrenceMutationResult,
  PassportRideOccurrencePage
} from '@app/models/passport/passport-ride-occurrence.models';
import {
  PassportVisit,
  PassportVisitDate,
  PassportVisitPage,
  UpdatePassportVisitRequest
} from '@app/models/passport/passport-visit.models';
import {
  PassportAnonymousDraft,
  PassportAnonymousDraftPreview,
  PassportAnonymousImportChoice,
  PassportAnonymousImportDecision,
  PassportAnonymousImportReport,
  PassportAnonymousImportReportItem,
  PassportAnonymousMetadataChoice,
  PassportAnonymousRideDraft,
  PassportAnonymousServerRidePreview
} from '../models/passport-anonymous-draft.models';
import {
  PASSPORT_ANONYMOUS_DRAFT_STORE_PORT,
  PassportAnonymousDraftStorePort
} from './passport-anonymous-draft-store.ports';
import {
  PASSPORT_ANONYMOUS_IMPORT_OCCURRENCES_PORT,
  PASSPORT_ANONYMOUS_IMPORT_VISITS_PORT,
  PassportAnonymousImportOccurrencesPort,
  PassportAnonymousImportVisitsPort
} from './passport-anonymous-import-data.ports';

@Injectable()
export class PassportAnonymousImportStateFacade {
  private static readonly PageSize: number = 100;
  private static readonly MaximumPages: number = 20;

  private readonly previewsSignal = signal<PassportAnonymousDraftPreview[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly importingSignal = signal<boolean>(false);
  private readonly comparingSignal = signal<boolean>(false);
  private readonly comparisonPreparedSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly reportSignal = signal<PassportAnonymousImportReport | null>(null);

  readonly previews: Signal<PassportAnonymousDraftPreview[]> = this.previewsSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly importing: Signal<boolean> = this.importingSignal.asReadonly();
  readonly comparing: Signal<boolean> = this.comparingSignal.asReadonly();
  readonly comparisonPrepared: Signal<boolean> = this.comparisonPreparedSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  readonly report: Signal<PassportAnonymousImportReport | null> = this.reportSignal.asReadonly();

  constructor(
    @Inject(PASSPORT_ANONYMOUS_DRAFT_STORE_PORT)
    private readonly store: PassportAnonymousDraftStorePort,
    @Inject(PASSPORT_ANONYMOUS_IMPORT_VISITS_PORT)
    private readonly visitsApi: PassportAnonymousImportVisitsPort,
    @Inject(PASSPORT_ANONYMOUS_IMPORT_OCCURRENCES_PORT)
    private readonly occurrencesApi: PassportAnonymousImportOccurrencesPort
  ) {
  }

  async load(): Promise<void> {
    if (!this.store.isAvailable()) {
      this.errorKeySignal.set('passport.anonymousDrafts.errors.storageUnavailable');
      return;
    }

    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);
    this.reportSignal.set(null);
    this.comparisonPreparedSignal.set(false);
    try {
      const drafts: PassportAnonymousDraft[] = await this.store.list();
      this.previewsSignal.set(drafts.map(
        (draft: PassportAnonymousDraft): PassportAnonymousDraftPreview => ({
          draft,
          similarVisits: [],
          selectedTarget: null,
          serverRides: null,
          decision: {
            draftId: draft.id,
            choice: 'Separate',
            targetVisitId: null,
            metadataChoice: 'KeepServer'
          }
        })));
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.import.errors.preview');
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async prepareComparison(consent: boolean): Promise<void> {
    if (!consent || this.comparingSignal() || this.comparisonPreparedSignal()) {
      if (!consent) {
        this.errorKeySignal.set('passport.anonymousDrafts.import.errors.consent');
      }
      return;
    }

    this.comparingSignal.set(true);
    this.errorKeySignal.set(null);
    try {
      const compared: PassportAnonymousDraftPreview[] = [];
      for (const preview of this.previewsSignal()) {
        const candidates: PassportVisit[] = await this.loadVisitCandidates(preview.draft);
        compared.push({
          ...preview,
          similarVisits: candidates.filter(
            (visit: PassportVisit): boolean => this.hasSameDate(
              visit.date,
              preview.draft.visit.date
            )
          )
        });
      }

      this.previewsSignal.set(compared);
      this.comparisonPreparedSignal.set(true);
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.import.errors.preview');
    } finally {
      this.comparingSignal.set(false);
    }
  }

  setChoice(draftId: string, choice: PassportAnonymousImportChoice): void {
    this.previewsSignal.update((previews: PassportAnonymousDraftPreview[]): PassportAnonymousDraftPreview[] =>
      previews.map((preview: PassportAnonymousDraftPreview): PassportAnonymousDraftPreview =>
        preview.draft.id === draftId
          ? {
              ...preview,
              selectedTarget: choice === 'Merge' ? preview.selectedTarget : null,
              serverRides: choice === 'Merge' ? preview.serverRides : null,
              decision: {
                ...preview.decision,
                choice,
                targetVisitId: choice === 'Merge' ? preview.decision.targetVisitId : null
              }
            }
          : preview));
  }

  async setTargetVisit(draftId: string, visitId: string): Promise<void> {
    this.errorKeySignal.set(null);
    const preview: PassportAnonymousDraftPreview | undefined = this.previewsSignal()
      .find((candidate: PassportAnonymousDraftPreview): boolean => candidate.draft.id === draftId);
    const selectedTarget: PassportVisit | undefined = preview?.similarVisits.find(
      (visit: PassportVisit): boolean => visit.id === visitId && visit.status === 'Draft'
    );
    if (!preview || !selectedTarget) {
      this.errorKeySignal.set('passport.anonymousDrafts.import.errors.invalidTarget');
      return;
    }

    this.updatePreview(draftId, (current: PassportAnonymousDraftPreview): PassportAnonymousDraftPreview => ({
      ...current,
      selectedTarget,
      serverRides: null,
      decision: {
        ...current.decision,
        choice: 'Merge',
        targetVisitId: selectedTarget.id
      }
    }));
    try {
      const occurrences: PassportRideOccurrence[] = await this.loadAllOccurrences(selectedTarget.id);
      const serverRides: PassportAnonymousServerRidePreview[] = occurrences.map(
        (occurrence: PassportRideOccurrence): PassportAnonymousServerRidePreview => ({
          id: occurrence.id,
          attractionName: occurrence.target?.name?.trim() || occurrence.parkItemId,
          status: occurrence.status,
          localTime: occurrence.moment.localTime,
          privateNote: occurrence.privateNote
        })
      );
      this.updatePreview(draftId, (current: PassportAnonymousDraftPreview): PassportAnonymousDraftPreview => ({
        ...current,
        serverRides
      }));
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.import.errors.comparison');
    }
  }

  setMetadataChoice(draftId: string, choice: PassportAnonymousMetadataChoice): void {
    this.updatePreview(draftId, (preview: PassportAnonymousDraftPreview): PassportAnonymousDraftPreview => ({
      ...preview,
      decision: { ...preview.decision, metadataChoice: choice }
    }));
  }

  canImport(): boolean {
    return this.comparisonPreparedSignal()
      && this.previewsSignal().length > 0
      && this.previewsSignal().every((preview: PassportAnonymousDraftPreview): boolean =>
        preview.decision.choice !== 'Merge'
        || (!!preview.selectedTarget && preview.selectedTarget.status === 'Draft'));
  }

  async importAll(consent: boolean): Promise<void> {
    if (!consent || !this.canImport() || this.importingSignal()) {
      this.errorKeySignal.set('passport.anonymousDrafts.import.errors.consent');
      return;
    }

    this.importingSignal.set(true);
    this.errorKeySignal.set(null);
    this.reportSignal.set(null);
    const results: PassportAnonymousImportReportItem[] = [];
    for (const preview of this.previewsSignal()) {
      results.push(await this.importDraft(preview));
    }

    const report: PassportAnonymousImportReport = {
      items: results,
      importedVisitCount: results.filter(
        (item: PassportAnonymousImportReportItem): boolean => item.outcome === 'Imported'
      ).length,
      mergedVisitCount: results.filter(
        (item: PassportAnonymousImportReportItem): boolean => item.outcome === 'Merged'
      ).length,
      importedRideCount: results.reduce(
        (count: number, item: PassportAnonymousImportReportItem): number =>
          count + item.importedRideCount,
        0
      ),
      ignoredCount: results.filter(
        (item: PassportAnonymousImportReportItem): boolean => item.outcome === 'Ignored'
      ).length,
      failedCount: results.filter(
        (item: PassportAnonymousImportReportItem): boolean => item.outcome === 'Failed'
      ).length
    };
    this.reportSignal.set(report);
    const retainedDraftIds: Set<string> = new Set<string>(
      results
        .filter((item: PassportAnonymousImportReportItem): boolean =>
          item.outcome === 'Failed' || item.outcome === 'Ignored')
        .map((item: PassportAnonymousImportReportItem): string => item.draftId)
    );
    this.previewsSignal.update((previews: PassportAnonymousDraftPreview[]): PassportAnonymousDraftPreview[] =>
      previews.filter((preview: PassportAnonymousDraftPreview): boolean =>
        retainedDraftIds.has(preview.draft.id)));
    this.importingSignal.set(false);
  }

  totalRideCount(): number {
    return this.previewsSignal().reduce(
      (total: number, preview: PassportAnonymousDraftPreview): number =>
        total + this.draftRideCount(preview.draft),
      0
    );
  }

  draftRideCount(draft: PassportAnonymousDraft): number {
    return draft.rides.reduce(
      (total: number, ride: PassportAnonymousRideDraft): number => total + ride.count,
      0
    );
  }

  private async importDraft(
    preview: PassportAnonymousDraftPreview
  ): Promise<PassportAnonymousImportReportItem> {
    if (preview.decision.choice === 'Ignore') {
      return this.reportItem(preview, 'Ignored', null, 0, null);
    }

    try {
      const target: PassportVisit = preview.decision.choice === 'Separate'
        ? await this.createVisit(preview.draft)
        : await this.prepareMergeTarget(preview);
      const importedRideCount: number = await this.importRides(
        preview.draft,
        target.id,
        preview.decision
      );
      await this.store.delete(preview.draft.id);
      return this.reportItem(
        preview,
        preview.decision.choice === 'Separate' ? 'Imported' : 'Merged',
        target.id,
        importedRideCount,
        null
      );
    } catch {
      return this.reportItem(
        preview,
        'Failed',
        preview.decision.targetVisitId,
        0,
        'passport.anonymousDrafts.import.errors.itemFailed'
      );
    }
  }

  private async createVisit(draft: PassportAnonymousDraft): Promise<PassportVisit> {
    const created: PassportVisit = await firstValueFrom(
      this.visitsApi.createVisit(draft.visit, draft.visitOperationId)
    );
    if (created.parkId !== draft.visit.parkId
      || !this.hasSameDate(created.date, draft.visit.date)) {
      throw new Error('passport-anonymous-import.visit-ack-mismatch');
    }

    return created;
  }

  private async prepareMergeTarget(
    preview: PassportAnonymousDraftPreview
  ): Promise<PassportVisit> {
    const selectedTarget: PassportVisit | null = preview.selectedTarget;
    if (!selectedTarget || selectedTarget.status !== 'Draft') {
      throw new Error('passport-anonymous-import.target-required');
    }

    if (preview.decision.metadataChoice === 'KeepServer') {
      return selectedTarget;
    }

    const request: UpdatePassportVisitRequest = {
      date: preview.draft.visit.date,
      timeZoneId: preview.draft.visit.timeZoneId,
      serviceDayConvention: preview.draft.visit.serviceDayConvention,
      title: preview.draft.visit.title,
      privateNote: preview.draft.visit.privateNote,
      expectedVersion: selectedTarget.version
    };
    try {
      const updated: PassportVisit = await firstValueFrom(
        this.visitsApi.updateVisit(selectedTarget.id, request)
      );
      if (!this.matchesMetadata(updated, preview.draft)) {
        throw new Error('passport-anonymous-import.metadata-ack-mismatch');
      }

      return updated;
    } catch {
      const recovered: PassportVisit = await firstValueFrom(
        this.visitsApi.getVisit(selectedTarget.id)
      );
      if (!this.matchesMetadata(recovered, preview.draft)) {
        throw new Error('passport-anonymous-import.metadata-conflict');
      }

      return recovered;
    }
  }

  private async importRides(
    draft: PassportAnonymousDraft,
    targetVisitId: string,
    decision: PassportAnonymousImportDecision
  ): Promise<number> {
    const expectedCount: number = this.draftRideCount(draft);
    if (expectedCount === 0) {
      return 0;
    }

    const items: CreatePassportRideOccurrenceBatchItem[] = draft.rides.flatMap(
      (ride: PassportAnonymousRideDraft): CreatePassportRideOccurrenceBatchItem[] =>
        Array.from({ length: ride.count }, (): CreatePassportRideOccurrenceBatchItem => ({
          parkItemId: ride.parkItemId,
          moment: ride.moment,
          status: ride.status,
          privateNote: ride.privateNote,
          confirmHistoricalConflict: ride.confirmHistoricalConflict,
          count: 1
        }))
    );
    const operationBase: string = decision.choice === 'Merge'
      ? `${draft.rideOperationId}:merge:${targetVisitId}`
      : draft.rideOperationId;
    for (let offset: number = 0; offset < items.length; offset += 100) {
      const chunk: CreatePassportRideOccurrenceBatchItem[] = items.slice(offset, offset + 100);
      const chunkIndex: number = Math.trunc(offset / 100);
      const operationId: string = items.length <= 100
        ? operationBase.slice(0, 128)
        : `${operationBase.slice(0, 118)}:part:${chunkIndex}`;
      const result: PassportRideOccurrenceMutationResult = await firstValueFrom(
        this.occurrencesApi.importBatch(targetVisitId, { items: chunk }, operationId)
      );
      if (result.occurrences.length !== chunk.length
        || result.occurrences.some((occurrence: PassportRideOccurrence): boolean =>
          occurrence.visitId !== targetVisitId)) {
        throw new Error('passport-anonymous-import.ride-ack-mismatch');
      }
    }

    return expectedCount;
  }

  private async loadVisitCandidates(draft: PassportAnonymousDraft): Promise<PassportVisit[]> {
    const visits: PassportVisit[] = [];
    let cursor: string | null = null;
    for (let page: number = 0; page < PassportAnonymousImportStateFacade.MaximumPages; page += 1) {
      const result: PassportVisitPage = await firstValueFrom(
        this.visitsApi.listVisits(
          PassportAnonymousImportStateFacade.PageSize,
          cursor,
          { parkId: draft.visit.parkId, year: draft.visit.date.year }
        )
      );
      visits.push(...result.items);
      cursor = result.nextCursor;
      if (!cursor) {
        return visits;
      }
    }

    throw new Error('passport-anonymous-import.preview-too-large');
  }

  private async loadAllOccurrences(visitId: string): Promise<PassportRideOccurrence[]> {
    const occurrences: PassportRideOccurrence[] = [];
    let cursor: string | null = null;
    for (let page: number = 0; page < PassportAnonymousImportStateFacade.MaximumPages; page += 1) {
      const result: PassportRideOccurrencePage = await firstValueFrom(
        this.occurrencesApi.list(
          visitId,
          cursor,
          PassportAnonymousImportStateFacade.PageSize
        )
      );
      occurrences.push(...result.items);
      cursor = result.nextCursor;
      if (!cursor) {
        return occurrences;
      }
    }

    throw new Error('passport-anonymous-import.comparison-too-large');
  }

  private hasSameDate(left: PassportVisitDate, right: PassportVisitDate): boolean {
    return left.year === right.year
      && left.month === right.month
      && left.day === right.day
      && left.precision === right.precision;
  }

  private matchesMetadata(visit: PassportVisit, draft: PassportAnonymousDraft): boolean {
    return visit.parkId === draft.visit.parkId
      && this.hasSameDate(visit.date, draft.visit.date)
      && visit.timeZoneId === draft.visit.timeZoneId
      && visit.serviceDayConvention === draft.visit.serviceDayConvention
      && visit.title === draft.visit.title
      && visit.privateNote === draft.visit.privateNote;
  }

  private updatePreview(
    draftId: string,
    update: (preview: PassportAnonymousDraftPreview) => PassportAnonymousDraftPreview
  ): void {
    this.previewsSignal.update((previews: PassportAnonymousDraftPreview[]): PassportAnonymousDraftPreview[] =>
      previews.map((preview: PassportAnonymousDraftPreview): PassportAnonymousDraftPreview =>
        preview.draft.id === draftId ? update(preview) : preview));
  }

  private reportItem(
    preview: PassportAnonymousDraftPreview,
    outcome: PassportAnonymousImportReportItem['outcome'],
    serverVisitId: string | null,
    importedRideCount: number,
    errorKey: string | null
  ): PassportAnonymousImportReportItem {
    return {
      draftId: preview.draft.id,
      parkName: preview.draft.parkName,
      outcome,
      serverVisitId,
      importedRideCount,
      errorKey
    };
  }
}
