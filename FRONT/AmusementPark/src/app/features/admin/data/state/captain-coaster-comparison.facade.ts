import { DestroyRef, Injectable, Signal, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  CaptainCoasterComparisonPagedResponse,
  CaptainCoasterComparisonResultResponse,
  CaptainCoasterDuplicateResolutionRequest,
  CaptainCoasterExternalVariantResponse,
  CaptainCoasterFieldChangeResponse,
  CaptainCoasterFieldResolutionRequest,
  CaptainCoasterSessionResponse,
  ComparisonFilters
} from '@app/models/admin/data/data-management.models';
import { DataSourcesApiService } from '@data-access/admin/data-sources-api.service';
import { CaptainCoasterPipelineFacade } from './captain-coaster-pipeline.facade';

interface DuplicateResolutionState {
  strategy: 'SelectVariant' | 'Merge';
  selectedExternalVariantId: string | null;
  fieldSelections: Record<string, string>;
}

interface RawCaptainCoasterComparisonPagedResponse {
  items?: RawCaptainCoasterComparisonResultResponse[];
  Items?: RawCaptainCoasterComparisonResultResponse[];
  totalCount?: number;
  TotalCount?: number;
  page?: number;
  Page?: number;
  pageSize?: number;
  PageSize?: number;
  sessionUpdatedCount?: number;
  SessionUpdatedCount?: number;
  sessionMissingCount?: number;
  SessionMissingCount?: number;
  sessionDuplicateCount?: number;
  SessionDuplicateCount?: number;
  sessionAppliedCount?: number;
  SessionAppliedCount?: number;
}

interface RawCaptainCoasterComparisonResultResponse {
  id?: string;
  Id?: string;
  entityType?: string;
  EntityType?: string;
  changeType?: string;
  ChangeType?: string;
  displayName?: string;
  DisplayName?: string;
  localEntityId?: string | null;
  LocalEntityId?: string | null;
  externalEntityId?: string | null;
  ExternalEntityId?: string | null;
  matchConfidence?: string;
  MatchConfidence?: string;
  isApplied?: boolean;
  IsApplied?: boolean;
  hasExternalDuplicates?: boolean;
  HasExternalDuplicates?: boolean;
  requiresManualResolution?: boolean;
  RequiresManualResolution?: boolean;
  resolutionStatus?: string;
  ResolutionStatus?: string;
  appliedExternalVariantId?: string | null;
  AppliedExternalVariantId?: string | null;
  changes?: RawCaptainCoasterFieldChangeResponse[];
  Changes?: RawCaptainCoasterFieldChangeResponse[];
  externalVariants?: RawCaptainCoasterExternalVariantResponse[];
  ExternalVariants?: RawCaptainCoasterExternalVariantResponse[];
}

interface RawCaptainCoasterExternalVariantResponse {
  externalVariantId?: string;
  ExternalVariantId?: string;
  displayLabel?: string;
  DisplayLabel?: string;
  candidateLocalEntityId?: string | null;
  CandidateLocalEntityId?: string | null;
  sourceUrl?: string | null;
  SourceUrl?: string | null;
  isSuggested?: boolean;
  IsSuggested?: boolean;
  changes?: RawCaptainCoasterFieldChangeResponse[];
  Changes?: RawCaptainCoasterFieldChangeResponse[];
}

interface RawCaptainCoasterFieldChangeResponse {
  field?: string;
  Field?: string;
  localValue?: string | null;
  LocalValue?: string | null;
  externalValue?: string | null;
  ExternalValue?: string | null;
  isDifferent?: boolean;
  IsDifferent?: boolean;
}

@Injectable()
export class CaptainCoasterComparisonFacade {
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly pagedResultSignal = signal<CaptainCoasterComparisonPagedResponse | null>(null);
  private readonly isLoadedSignal = signal(false);
  private readonly isLoadingPageSignal = signal(false);
  private readonly currentPageSignal = signal(0);
  private readonly pageSizeSignal = signal(50);
  private readonly filtersSignal = signal<ComparisonFilters>({
    entityType: null,
    changeType: null,
    isApplied: null
  });
  private readonly selectedCountSignal = signal(0);
  private readonly allPageSelectedSignal = signal(false);

  private readonly duplicateResolutionStateByResultId: Map<string, DuplicateResolutionState> = new Map<string, DuplicateResolutionState>();
  private readonly selectedIdsSet: Set<string> = new Set<string>();

  public readonly pagedResult: Signal<CaptainCoasterComparisonPagedResponse | null> = this.pagedResultSignal.asReadonly();
  public readonly isLoaded: Signal<boolean> = this.isLoadedSignal.asReadonly();
  public readonly isLoadingPage: Signal<boolean> = this.isLoadingPageSignal.asReadonly();
  public readonly currentPage: Signal<number> = this.currentPageSignal.asReadonly();
  public readonly pageSize: Signal<number> = this.pageSizeSignal.asReadonly();
  public readonly filters: Signal<ComparisonFilters> = this.filtersSignal.asReadonly();
  public readonly selectedCount: Signal<number> = this.selectedCountSignal.asReadonly();
  public readonly allPageSelected: Signal<boolean> = this.allPageSelectedSignal.asReadonly();

  public readonly sessionUpdated = computed(() => this.pagedResultSignal()?.sessionUpdatedCount ?? 0);
  public readonly sessionMissing = computed(() => this.pagedResultSignal()?.sessionMissingCount ?? 0);
  public readonly sessionDuplicate = computed(() => this.pagedResultSignal()?.sessionDuplicateCount ?? 0);
  public readonly sessionApplied = computed(() => this.pagedResultSignal()?.sessionAppliedCount ?? 0);
  public readonly totalCount = computed(() => this.pagedResultSignal()?.totalCount ?? 0);
  public readonly currentItems = computed(() => this.pagedResultSignal()?.items ?? []);

  public readonly entityTypeOptions = [
    { label: 'Tous les types', value: null },
    { label: 'Parc', value: 'Park' },
    { label: 'Coaster', value: 'Coaster' }
  ];

  public readonly changeTypeOptions = [
    { label: 'Tous les changements', value: null },
    { label: 'Nouveaux', value: 'MissingLocal' },
    { label: 'Modifiés', value: 'Updated' },
    { label: 'Doublons externes', value: 'DuplicateExternal' }
  ];

  public readonly appliedOptions = [
    { label: 'Tous', value: null },
    { label: 'Non appliqués', value: false },
    { label: 'Déjà appliqués', value: true }
  ];

  constructor(
    private readonly dataSourcesApiService: DataSourcesApiService,
    private readonly captainCoasterPipelineFacade: CaptainCoasterPipelineFacade
  ) {
    let hasObservedInitialGeneration = false;

    effect(() => {
      this.captainCoasterPipelineFacade.comparisonGeneration();

      if (!hasObservedInitialGeneration) {
        hasObservedInitialGeneration = true;
        return;
      }

      this.resetComparisonState();
    }, { allowSignalWrites: true });
  }

  async loadComparisonAsync(): Promise<void> {
    const sessionId: string | null = this.captainCoasterPipelineFacade.session()?.id ?? null;
    this.currentPageSignal.set(0);
    this.resetSelection();
    this.captainCoasterPipelineFacade.clearFeedbackMessages();

    try {
      await this.fetchPageAsync(sessionId, this.filtersSignal(), 0, this.pageSizeSignal());
      this.isLoadedSignal.set(true);
    } catch (error: unknown) {
      this.captainCoasterPipelineFacade.setErrorMessage(this.extractErrorMessage(error));
    }
  }

  async onPageChange(event: { first?: number; rows?: number }): Promise<void> {
    const first: number = event.first ?? 0;
    const rows: number = event.rows ?? this.pageSizeSignal();
    const newPage: number = Math.floor(first / rows);
    const newPageSize: number = rows;
    this.currentPageSignal.set(newPage);
    this.pageSizeSignal.set(newPageSize);
    this.resetSelection();
    await this.fetchPageAsync(this.captainCoasterPipelineFacade.session()?.id ?? null, this.filtersSignal(), newPage, newPageSize);
  }

  setEntityTypeFilter(value: string | null): void {
    const current: ComparisonFilters = this.filtersSignal();
    this.filtersSignal.set({ ...current, entityType: value });
    void this.onFilterChangeAsync();
  }

  setChangeTypeFilter(value: string | null): void {
    const current: ComparisonFilters = this.filtersSignal();
    this.filtersSignal.set({ ...current, changeType: value });
    void this.onFilterChangeAsync();
  }

  setAppliedFilter(value: boolean | null): void {
    const current: ComparisonFilters = this.filtersSignal();
    this.filtersSignal.set({ ...current, isApplied: value });
    void this.onFilterChangeAsync();
  }

  toggleSelection(id: string, checked: boolean): void {
    if (checked) {
      this.selectedIdsSet.add(id);
    } else {
      this.selectedIdsSet.delete(id);
    }

    this.syncSelectionSignals();
  }

  isSelected(id: string): boolean {
    return this.selectedIdsSet.has(id);
  }

  toggleSelectPage(checked: boolean): void {
    const items: CaptainCoasterComparisonResultResponse[] = this.currentItems();
    if (checked) {
      items
        .filter((item: CaptainCoasterComparisonResultResponse) => !item.isApplied)
        .forEach((item: CaptainCoasterComparisonResultResponse) => this.selectedIdsSet.add(item.id));
    } else {
      items.forEach((item: CaptainCoasterComparisonResultResponse) => this.selectedIdsSet.delete(item.id));
    }

    this.syncSelectionSignals();
  }

  async applySelectionAsync(): Promise<void> {
    if (this.selectedIdsSet.size === 0) {
      this.captainCoasterPipelineFacade.setErrorMessage('Sélectionnez au moins une ligne.');
      return;
    }

    const sessionId: string | null = this.captainCoasterPipelineFacade.session()?.id ?? null;
    if (sessionId === null) {
      this.captainCoasterPipelineFacade.setErrorMessage('Aucune session Captain Coaster sélectionnée.');
      return;
    }

    const selectedRows: CaptainCoasterComparisonResultResponse[] = this.currentItems()
      .filter((item: CaptainCoasterComparisonResultResponse) => this.selectedIdsSet.has(item.id));
    const duplicateResolutions: CaptainCoasterDuplicateResolutionRequest[] = this.buildDuplicateResolutions(selectedRows);
    if (selectedRows.some((item: CaptainCoasterComparisonResultResponse) => item.requiresManualResolution) && duplicateResolutions.length === 0) {
      this.captainCoasterPipelineFacade.setErrorMessage('Résolvez les doublons sélectionnés avant de lancer l’application.');
      return;
    }

    this.captainCoasterPipelineFacade.setBusy(true);
    this.captainCoasterPipelineFacade.clearFeedbackMessages();
    this.captainCoasterPipelineFacade.startPolling(sessionId, 'apply');

    try {
      const response: { appliedCount: number } = await firstValueFrom(
        this.dataSourcesApiService.applySelectedIds(Array.from(this.selectedIdsSet), duplicateResolutions)
      );
      this.captainCoasterPipelineFacade.setSuccessMessage(`${response.appliedCount} changement(s) appliqué(s).`);
      this.resetSelection();
      await this.fetchPageAsync(sessionId, this.filtersSignal(), this.currentPageSignal(), this.pageSizeSignal());
      await this.captainCoasterPipelineFacade.refreshStatusAsync();
    } catch (error: unknown) {
      this.captainCoasterPipelineFacade.stopPolling();
      this.captainCoasterPipelineFacade.setErrorMessage(this.extractErrorMessage(error));
    } finally {
      this.captainCoasterPipelineFacade.setBusy(false);
    }
  }

  async applyAllAsync(entityTypeFilter: string | null = null, changeTypeFilter: string | null = null): Promise<void> {
    if (changeTypeFilter === 'DuplicateExternal') {
      this.captainCoasterPipelineFacade.setErrorMessage('Les doublons externes nécessitent une résolution manuelle ligne par ligne.');
      return;
    }

    const sessionId: string | null = this.captainCoasterPipelineFacade.session()?.id ?? null;
    if (sessionId === null) {
      this.captainCoasterPipelineFacade.setErrorMessage('Aucune session Captain Coaster sélectionnée.');
      return;
    }

    this.captainCoasterPipelineFacade.setBusy(true);
    this.captainCoasterPipelineFacade.clearFeedbackMessages();
    this.captainCoasterPipelineFacade.startPolling(sessionId, 'apply');

    try {
      const response: { appliedCount: number } = await firstValueFrom(
        this.dataSourcesApiService.applyAll(sessionId, entityTypeFilter, changeTypeFilter)
      );
      this.captainCoasterPipelineFacade.setSuccessMessage(
        `${response.appliedCount} changement(s) appliqué(s) au total. Les doublons externes restent à arbitrer manuellement.`
      );
      this.resetSelection();
      await this.fetchPageAsync(sessionId, this.filtersSignal(), this.currentPageSignal(), this.pageSizeSignal());
      await this.captainCoasterPipelineFacade.refreshStatusAsync();
    } catch (error: unknown) {
      this.captainCoasterPipelineFacade.stopPolling();
      this.captainCoasterPipelineFacade.setErrorMessage(this.extractErrorMessage(error));
    } finally {
      this.captainCoasterPipelineFacade.setBusy(false);
    }
  }

  getChangeTypeSeverity(changeType: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    if (changeType === 'Updated') {
      return 'warn';
    }

    if (changeType === 'MissingLocal') {
      return 'info';
    }

    if (changeType === 'DuplicateExternal') {
      return 'danger';
    }

    return 'secondary';
  }

  trackById(_index: number, item: CaptainCoasterComparisonResultResponse): string {
    return item.id;
  }

  isDuplicateRow(item: CaptainCoasterComparisonResultResponse): boolean {
    return item.requiresManualResolution || item.hasExternalDuplicates;
  }

  getDuplicateState(item: CaptainCoasterComparisonResultResponse): DuplicateResolutionState {
    this.ensureDuplicateResolutionState(item);
    return this.duplicateResolutionStateByResultId.get(item.id)!;
  }

  setDuplicateStrategy(item: CaptainCoasterComparisonResultResponse, strategy: 'SelectVariant' | 'Merge'): void {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    state.strategy = strategy;
    if (state.selectedExternalVariantId === null) {
      state.selectedExternalVariantId = this.getSuggestedVariantId(item);
    }
  }

  setSelectedVariant(item: CaptainCoasterComparisonResultResponse, value: string | null): void {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    state.selectedExternalVariantId = value;

    if (value !== null) {
      const fields: string[] = this.getMergeEligibleFields(item);
      for (const field of fields) {
        if (!(field in state.fieldSelections)) {
          state.fieldSelections[field] = `variant:${value}`;
        }
      }
    }
  }

  getMergeEligibleFields(item: CaptainCoasterComparisonResultResponse): string[] {
    const fields: Set<string> = new Set<string>();
    for (const variant of item.externalVariants) {
      for (const change of variant.changes) {
        if (this.isMergeEligibleField(change.field)) {
          fields.add(change.field);
        }
      }
    }

    const order: string[] = [
      'name',
      'countryCode',
      'parkName',
      'manufacturer',
      'model',
      'sourceUrl',
      'status',
      'materialType',
      'seatingType',
      'launchType',
      'restraintType',
      'isLaunched',
      'openingDate',
      'closingDate',
      'heightInMeters',
      'lengthInMeters',
      'speedInKmH',
      'inversionCount'
    ];

    return Array.from(fields).sort((left: string, right: string) => {
      const leftIndex: number = order.indexOf(left);
      const rightIndex: number = order.indexOf(right);
      const safeLeftIndex: number = leftIndex === -1 ? 999 : leftIndex;
      const safeRightIndex: number = rightIndex === -1 ? 999 : rightIndex;
      if (safeLeftIndex !== safeRightIndex) {
        return safeLeftIndex - safeRightIndex;
      }

      return left.localeCompare(right);
    });
  }

  getMergeSelection(item: CaptainCoasterComparisonResultResponse, field: string): string {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    return state.fieldSelections[field] ?? `variant:${this.getSuggestedVariantId(item) ?? ''}`;
  }

  setMergeSelection(item: CaptainCoasterComparisonResultResponse, field: string, value: string): void {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    state.fieldSelections[field] = value;
  }

  getMergeOptions(item: CaptainCoasterComparisonResultResponse, field: string): { label: string; value: string }[] {
    const options: { label: string; value: string }[] = [{ label: 'Conserver la valeur locale', value: 'local' }];
    for (const variant of item.externalVariants) {
      const preview: string = this.getExternalFieldValue(variant, field) ?? '∅';
      options.push({
        label: `${variant.displayLabel} → ${preview}`,
        value: `variant:${variant.externalVariantId}`
      });
    }

    return options;
  }

  getResolutionSummary(item: CaptainCoasterComparisonResultResponse): string {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    if (state.strategy === 'SelectVariant') {
      const variant: CaptainCoasterExternalVariantResponse | undefined = item.externalVariants
        .find((entry: CaptainCoasterExternalVariantResponse) => entry.externalVariantId === state.selectedExternalVariantId);
      return variant?.displayLabel ?? 'Aucune variante sélectionnée';
    }

    return `Fusion (${this.getMergeEligibleFields(item).length} champ(s))`;
  }

  private async onFilterChangeAsync(): Promise<void> {
    this.currentPageSignal.set(0);
    this.resetSelection();
    await this.fetchPageAsync(this.captainCoasterPipelineFacade.session()?.id ?? null, this.filtersSignal(), 0, this.pageSizeSignal());
  }

  private async fetchPageAsync(
    sessionId: string | null,
    filters: ComparisonFilters,
    page: number,
    pageSize: number
  ): Promise<void> {
    this.isLoadingPageSignal.set(true);
    try {
      let result: CaptainCoasterComparisonPagedResponse = this.normalizeComparisonPage(await firstValueFrom(
        this.dataSourcesApiService.getComparisonResults(sessionId, filters, page, pageSize)
      ));

      const session: CaptainCoasterSessionResponse | null = this.captainCoasterPipelineFacade.session();
      const hasActiveFilter: boolean = filters.entityType !== null || filters.changeType !== null || filters.isApplied !== null;
      if (sessionId !== null && !hasActiveFilter && result.totalCount === 0 && (session?.comparisonResults ?? 0) > 0) {
        result = this.normalizeComparisonPage(await firstValueFrom(
          this.dataSourcesApiService.getComparisonResults(null, filters, page, pageSize)
        ));
      }

      this.pagedResultSignal.set(result);
      this.ensureDuplicateResolutionStates(result.items);
      this.syncSelectionSignals();
    } finally {
      this.isLoadingPageSignal.set(false);
    }
  }

  private normalizeComparisonPage(
    rawResult: CaptainCoasterComparisonPagedResponse | RawCaptainCoasterComparisonPagedResponse
  ): CaptainCoasterComparisonPagedResponse {
    const normalizedRawResult: RawCaptainCoasterComparisonPagedResponse = rawResult as RawCaptainCoasterComparisonPagedResponse;
    const rawItems: RawCaptainCoasterComparisonResultResponse[] = (normalizedRawResult.items ?? normalizedRawResult.Items ?? []) as RawCaptainCoasterComparisonResultResponse[];

    return {
      items: rawItems.map((item: RawCaptainCoasterComparisonResultResponse) => this.normalizeComparisonItem(item)),
      totalCount: normalizedRawResult.totalCount ?? normalizedRawResult.TotalCount ?? rawItems.length,
      page: normalizedRawResult.page ?? normalizedRawResult.Page ?? this.currentPageSignal(),
      pageSize: normalizedRawResult.pageSize ?? normalizedRawResult.PageSize ?? this.pageSizeSignal(),
      sessionUpdatedCount: normalizedRawResult.sessionUpdatedCount ?? normalizedRawResult.SessionUpdatedCount ?? 0,
      sessionMissingCount: normalizedRawResult.sessionMissingCount ?? normalizedRawResult.SessionMissingCount ?? 0,
      sessionDuplicateCount: normalizedRawResult.sessionDuplicateCount ?? normalizedRawResult.SessionDuplicateCount ?? 0,
      sessionAppliedCount: normalizedRawResult.sessionAppliedCount ?? normalizedRawResult.SessionAppliedCount ?? 0
    };
  }

  private normalizeComparisonItem(rawItem: RawCaptainCoasterComparisonResultResponse): CaptainCoasterComparisonResultResponse {
    const rawChanges: RawCaptainCoasterFieldChangeResponse[] = rawItem.changes ?? rawItem.Changes ?? [];
    const rawVariants: RawCaptainCoasterExternalVariantResponse[] = rawItem.externalVariants ?? rawItem.ExternalVariants ?? [];

    return {
      id: rawItem.id ?? rawItem.Id ?? '',
      entityType: rawItem.entityType ?? rawItem.EntityType ?? '',
      changeType: rawItem.changeType ?? rawItem.ChangeType ?? '',
      displayName: rawItem.displayName ?? rawItem.DisplayName ?? '',
      localEntityId: rawItem.localEntityId ?? rawItem.LocalEntityId ?? null,
      externalEntityId: rawItem.externalEntityId ?? rawItem.ExternalEntityId ?? null,
      matchConfidence: rawItem.matchConfidence ?? rawItem.MatchConfidence ?? '',
      isApplied: rawItem.isApplied ?? rawItem.IsApplied ?? false,
      hasExternalDuplicates: rawItem.hasExternalDuplicates ?? rawItem.HasExternalDuplicates ?? false,
      requiresManualResolution: rawItem.requiresManualResolution ?? rawItem.RequiresManualResolution ?? false,
      resolutionStatus: rawItem.resolutionStatus ?? rawItem.ResolutionStatus ?? '',
      appliedExternalVariantId: rawItem.appliedExternalVariantId ?? rawItem.AppliedExternalVariantId ?? null,
      changes: rawChanges.map((change: RawCaptainCoasterFieldChangeResponse) => this.normalizeFieldChange(change)),
      externalVariants: rawVariants.map((variant: RawCaptainCoasterExternalVariantResponse) => this.normalizeExternalVariant(variant))
    };
  }

  private normalizeExternalVariant(rawVariant: RawCaptainCoasterExternalVariantResponse): CaptainCoasterExternalVariantResponse {
    const rawChanges: RawCaptainCoasterFieldChangeResponse[] = rawVariant.changes ?? rawVariant.Changes ?? [];

    return {
      externalVariantId: rawVariant.externalVariantId ?? rawVariant.ExternalVariantId ?? '',
      displayLabel: rawVariant.displayLabel ?? rawVariant.DisplayLabel ?? '',
      candidateLocalEntityId: rawVariant.candidateLocalEntityId ?? rawVariant.CandidateLocalEntityId ?? null,
      sourceUrl: rawVariant.sourceUrl ?? rawVariant.SourceUrl ?? null,
      isSuggested: rawVariant.isSuggested ?? rawVariant.IsSuggested ?? false,
      changes: rawChanges.map((change: RawCaptainCoasterFieldChangeResponse) => this.normalizeFieldChange(change))
    };
  }

  private normalizeFieldChange(rawChange: RawCaptainCoasterFieldChangeResponse): CaptainCoasterFieldChangeResponse {
    return {
      field: rawChange.field ?? rawChange.Field ?? '',
      localValue: rawChange.localValue ?? rawChange.LocalValue ?? null,
      externalValue: rawChange.externalValue ?? rawChange.ExternalValue ?? null,
      isDifferent: rawChange.isDifferent ?? rawChange.IsDifferent ?? false
    };
  }

  private resetComparisonState(): void {
    this.isLoadedSignal.set(false);
    this.pagedResultSignal.set(null);
    this.currentPageSignal.set(0);
    this.pageSizeSignal.set(50);
    this.resetSelection();
    this.duplicateResolutionStateByResultId.clear();
  }

  private resetSelection(): void {
    this.selectedIdsSet.clear();
    this.syncSelectionSignals();
  }

  private syncSelectionSignals(): void {
    this.selectedCountSignal.set(this.selectedIdsSet.size);
    const items: CaptainCoasterComparisonResultResponse[] = this.currentItems();
    const unapplied: CaptainCoasterComparisonResultResponse[] = items.filter((item: CaptainCoasterComparisonResultResponse) => !item.isApplied);
    const allPageSelected: boolean = unapplied.length > 0 && unapplied.every((item: CaptainCoasterComparisonResultResponse) => this.selectedIdsSet.has(item.id));
    this.allPageSelectedSignal.set(allPageSelected);
  }

  private ensureDuplicateResolutionStates(items: CaptainCoasterComparisonResultResponse[]): void {
    for (const item of items) {
      this.ensureDuplicateResolutionState(item);
    }
  }

  private ensureDuplicateResolutionState(item: CaptainCoasterComparisonResultResponse): void {
    if (!item.requiresManualResolution) {
      return;
    }

    if (this.duplicateResolutionStateByResultId.has(item.id)) {
      return;
    }

    const suggestedVariantId: string | null = this.getSuggestedVariantId(item);
    const fieldSelections: Record<string, string> = {};
    for (const field of this.getMergeEligibleFields(item)) {
      fieldSelections[field] = suggestedVariantId === null ? 'local' : `variant:${suggestedVariantId}`;
    }

    this.duplicateResolutionStateByResultId.set(item.id, {
      strategy: 'SelectVariant',
      selectedExternalVariantId: suggestedVariantId,
      fieldSelections
    });
  }

  private getSuggestedVariantId(item: CaptainCoasterComparisonResultResponse): string | null {
    return item.externalVariants.find((variant: CaptainCoasterExternalVariantResponse) => variant.isSuggested)?.externalVariantId
      ?? item.externalVariants[0]?.externalVariantId
      ?? null;
  }

  private isMergeEligibleField(field: string): boolean {
    return field !== 'duplicateVariants'
      && field !== 'externalId'
      && field !== 'externalSource'
      && field !== 'heightInFeet'
      && field !== 'lengthInFeet'
      && field !== 'speedInMph';
  }

  private getExternalFieldValue(variant: CaptainCoasterExternalVariantResponse, field: string): string | null {
    const change: { externalValue: string | null } | undefined = variant.changes.find((item) => item.field === field);
    return change?.externalValue ?? null;
  }

  private buildDuplicateResolutions(items: CaptainCoasterComparisonResultResponse[]): CaptainCoasterDuplicateResolutionRequest[] {
    const resolutions: CaptainCoasterDuplicateResolutionRequest[] = [];

    for (const item of items) {
      if (!item.requiresManualResolution) {
        continue;
      }

      const state: DuplicateResolutionState | undefined = this.duplicateResolutionStateByResultId.get(item.id);
      if (!state) {
        continue;
      }

      if (state.strategy === 'SelectVariant') {
        if (state.selectedExternalVariantId === null) {
          continue;
        }

        resolutions.push({
          comparisonResultId: item.id,
          strategy: 'SelectVariant',
          selectedExternalVariantId: state.selectedExternalVariantId,
          fieldResolutions: []
        });
        continue;
      }

      const fieldResolutions: CaptainCoasterFieldResolutionRequest[] = this.getMergeEligibleFields(item)
        .map((field: string) => this.buildFieldResolution(field, state.fieldSelections[field] ?? 'local'));

      resolutions.push({
        comparisonResultId: item.id,
        strategy: 'Merge',
        selectedExternalVariantId: state.selectedExternalVariantId,
        fieldResolutions
      });
    }

    return resolutions;
  }

  private buildFieldResolution(field: string, rawSelection: string): CaptainCoasterFieldResolutionRequest {
    if (rawSelection === 'local') {
      return {
        field,
        sourceType: 'Local',
        externalVariantId: null
      };
    }

    const variantId: string = rawSelection.replace('variant:', '');
    return {
      field,
      sourceType: 'Variant',
      externalVariantId: variantId
    };
  }

  private extractErrorMessage(error: unknown): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const payload: unknown = (error as { error: unknown }).error;
      if (typeof payload === 'string' && payload.trim().length > 0) {
        return payload;
      }

      if (typeof payload === 'object' && payload !== null && 'message' in payload) {
        const message: unknown = (payload as { message?: unknown }).message;
        if (typeof message === 'string' && message.trim().length > 0) {
          return message;
        }
      }
    }

    return 'Une erreur est survenue.';
  }
}
