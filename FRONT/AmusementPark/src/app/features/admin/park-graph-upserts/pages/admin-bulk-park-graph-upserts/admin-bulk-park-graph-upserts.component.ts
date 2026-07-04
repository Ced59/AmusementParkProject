import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { concat, concatMap, finalize, interval, of, Subscription } from 'rxjs';

import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkAdminListFilters, ParkAdminListSortDirection, ParkAdminListSortField } from '@data-access/parks/parks-api-endpoints';
import { AdminReviewStatus, getAdminReviewStatusSeverity, getAdminReviewStatusTranslationKey } from '@app/models/admin/admin-review-status';
import {
  BulkParkGraphUpsertRequest,
  BulkParkGraphUpsertResult,
  ParkGraphBulkExportJob,
  ParkGraphBulkExportRequest,
  ParkGraphBulkSelectionMode,
  ParkGraphExportSection,
  ParkGraphUpsertChange,
  ParkGraphUpsertCounts
} from '@app/models/admin/park-graph-upsert.models';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkOpeningHoursAdminFilter, ParkOpeningHoursAdminStatus } from '@app/models/parks/park-opening-hours';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ParkType } from '@app/models/parks/park-type';
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { getParkAudienceClassificationTranslationKey, getParkTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { extractSafeDisplayErrorMessage } from '@shared/utils/security';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Tag } from '@shared/ui/primitives/tag';
import { TableLazyLoadEvent, TableModule } from '@shared/ui/primitives/table';
import { UiTemplate } from '@shared/ui/primitives/api';

type AdminParkSortOptionValue = `${ParkAdminListSortField}:${ParkAdminListSortDirection}`;
type ToastSeverity = 'success' | 'info' | 'warn' | 'error';
type JsonObject = Record<string, unknown>;

interface ParkGraphExportSectionOption {
  readonly value: ParkGraphExportSection;
  readonly labelKey: string;
}

interface BulkParkGraphUpsertMessageGroup {
  readonly entityType: string;
  readonly displayName: string;
  readonly messages: string[];
}

@Component({
  selector: 'app-admin-bulk-park-graph-upserts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonDirective,
    EmptyStateComponent,
    InputText,
    TableModule,
    Tag,
    UiTemplate
  ],
  templateUrl: './admin-bulk-park-graph-upserts.component.html',
  styleUrl: './admin-bulk-park-graph-upserts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminBulkParkGraphUpsertsComponent implements OnInit, OnDestroy {
  protected readonly sectionOptions: readonly ParkGraphExportSectionOption[] = [
    { value: 'ParkBasics', labelKey: 'admin.bulkParkGraphUpserts.sections.ParkBasics' },
    { value: 'ParkAudience', labelKey: 'admin.bulkParkGraphUpserts.sections.ParkAudience' },
    { value: 'ParkLocation', labelKey: 'admin.bulkParkGraphUpserts.sections.ParkLocation' },
    { value: 'ParkAdministration', labelKey: 'admin.bulkParkGraphUpserts.sections.ParkAdministration' },
    { value: 'ParkDescriptions', labelKey: 'admin.bulkParkGraphUpserts.sections.ParkDescriptions' },
    { value: 'ParkHomeFeature', labelKey: 'admin.bulkParkGraphUpserts.sections.ParkHomeFeature' },
    { value: 'OpeningHours', labelKey: 'admin.bulkParkGraphUpserts.sections.OpeningHours' },
    { value: 'References', labelKey: 'admin.bulkParkGraphUpserts.sections.References' },
    { value: 'Zones', labelKey: 'admin.bulkParkGraphUpserts.sections.Zones' },
    { value: 'Items', labelKey: 'admin.bulkParkGraphUpserts.sections.Items' },
    { value: 'Images', labelKey: 'admin.bulkParkGraphUpserts.sections.Images' },
    { value: 'History', labelKey: 'admin.bulkParkGraphUpserts.sections.History' }
  ];

  protected readonly mobileSortOptions: ReadonlyArray<{ value: AdminParkSortOptionValue; labelKey: string }> = [
    { value: 'default:asc', labelKey: 'admin.parks.sort.default' },
    { value: 'name:asc', labelKey: 'admin.parks.sort.nameAsc' },
    { value: 'parkItemsTotalCount:desc', labelKey: 'admin.parks.sort.totalDesc' },
    { value: 'parkItemsTotalCount:asc', labelKey: 'admin.parks.sort.totalAsc' },
    { value: 'parkItemsVisibleCount:desc', labelKey: 'admin.parks.sort.visibleDesc' },
    { value: 'parkItemsVisibleCount:asc', labelKey: 'admin.parks.sort.visibleAsc' },
    { value: 'openingHoursStatus:asc', labelKey: 'admin.parks.sort.openingHoursAttentionFirst' },
    { value: 'openingHoursStatus:desc', labelKey: 'admin.parks.sort.openingHoursReadyFirst' }
  ];

  protected parks: Park[] = [];
  protected totalRecords: number = 0;
  protected pageSize: number = 10;
  protected currentPage: number = 1;
  protected loading: boolean = false;
  protected searchQuery: string = '';
  protected localVisibilityFilter: boolean | null = null;
  protected localAdminReviewStatusFilter: AdminReviewStatus | null = null;
  protected localTypeFilter: ParkType | null = null;
  protected localAudienceClassificationFilter: ParkAudienceClassificationFilter | null = null;
  protected localCountryCodeFilter: string = '';
  protected localValidCoordinatesFilter: boolean | null = null;
  protected localOpeningHoursFilter: ParkOpeningHoursAdminFilter = 'all';
  protected localClosedFilter: ClosedEntityFilter = DEFAULT_CLOSED_ENTITY_FILTER;
  protected sortField: ParkAdminListSortField = 'default';
  protected sortDirection: ParkAdminListSortDirection = 'asc';
  protected selectedParkIds: string[] = [];
  protected selectionMode: ParkGraphBulkSelectionMode = 'filtered';
  protected selectedSections: ParkGraphExportSection[] = [];
  protected jsonText: string = '';
  protected replaceCollections: boolean = false;
  protected previewResult: BulkParkGraphUpsertResult | null = null;
  protected lastAppliedResult: BulkParkGraphUpsertResult | null = null;
  protected exportJob: ParkGraphBulkExportJob | null = null;
  protected isExporting: boolean = false;
  protected isDownloadingExport: boolean = false;
  protected isPreviewing: boolean = false;
  protected isApplying: boolean = false;
  protected uiError: string | null = null;
  protected operationErrorDetail: string | null = null;

  private listRequestId: number = 0;
  private exportPollingSubscription: Subscription | null = null;
  private bulkDownloadSubscription: Subscription | null = null;

  constructor(
    private readonly parksApi: ParksApiService,
    private readonly parkGraphUpsertsApi: ParkGraphUpsertsApiService,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
  }

  ngOnInit(): void {
    this.loadParks(1, this.pageSize);
  }

  ngOnDestroy(): void {
    this.stopExportPolling();
    this.bulkDownloadSubscription?.unsubscribe();
  }

  protected search(): void {
    this.selectedParkIds = [];
    this.loadParks(1, this.pageSize);
  }

  protected clearSearch(): void {
    if (!this.canClearSearch) {
      return;
    }

    this.searchQuery = '';
    this.selectedParkIds = [];
    this.loadParks(1, this.pageSize);
  }

  protected onFiltersChanged(): void {
    this.selectedParkIds = [];
    this.loadParks(1, this.pageSize);
  }

  protected onPageChanged(event: TableLazyLoadEvent): void {
    const rows: number = event.rows ?? this.pageSize;
    const first: number = event.first ?? 0;
    const requestedPage: number = Math.floor(first / rows) + 1;
    const sortChanged: boolean = this.updateSort(
      this.normalizeSortField(event.sortField),
      this.normalizeSortDirection(event.sortOrder)
    );
    const page: number = sortChanged ? 1 : requestedPage;
    this.loadParks(page, rows);
  }

  protected onMobileSortChanged(value: string): void {
    const parts: string[] = value.split(':');
    const sortChanged: boolean = this.updateSort(
      this.normalizeSortField(parts[0]),
      parts[1] === 'desc' ? 'desc' : 'asc'
    );
    if (!sortChanged) {
      return;
    }

    this.loadParks(1, this.pageSize);
  }

  protected onParkSelectionChange(park: Park, event: Event): void {
    if (!park.id) {
      return;
    }

    const selected: boolean = (event.target as HTMLInputElement).checked;
    if (selected) {
      this.selectedParkIds = this.selectedParkIds.includes(park.id)
        ? this.selectedParkIds
        : [...this.selectedParkIds, park.id];
      return;
    }

    this.selectedParkIds = this.selectedParkIds.filter((parkId: string): boolean => parkId !== park.id);
  }

  protected onAllSelectionChange(event: Event): void {
    const selected: boolean = (event.target as HTMLInputElement).checked;
    const visibleIds: string[] = this.parks.map((park: Park): string | undefined => park.id).filter((parkId: string | undefined): parkId is string => !!parkId);

    if (!selected) {
      this.selectedParkIds = this.selectedParkIds.filter((parkId: string): boolean => !visibleIds.includes(parkId));
      return;
    }

    this.selectedParkIds = Array.from(new Set([...this.selectedParkIds, ...visibleIds]));
  }

  protected clearSelection(): void {
    this.selectedParkIds = [];
  }

  protected toggleSection(section: ParkGraphExportSection, event: Event): void {
    const selected: boolean = (event.target as HTMLInputElement).checked;
    if (selected) {
      this.selectedSections = this.selectedSections.includes(section)
        ? this.selectedSections
        : [...this.selectedSections, section];
      return;
    }

    this.selectedSections = this.selectedSections.filter((candidate: ParkGraphExportSection): boolean => candidate !== section);
  }

  protected selectAllSections(): void {
    this.selectedSections = this.sectionOptions.map((option: ParkGraphExportSectionOption): ParkGraphExportSection => option.value);
  }

  protected clearSections(): void {
    this.selectedSections = [];
  }

  protected exportBulkJson(): void {
    if (!this.canExport) {
      return;
    }

    this.uiError = null;
    this.operationErrorDetail = null;
    this.exportJob = null;
    this.isExporting = true;
    this.changeDetectorRef.markForCheck();

    const request: ParkGraphBulkExportRequest = this.buildExportRequest();
    this.parkGraphUpsertsApi.startBulkParkExportJob(request)
      .subscribe({
        next: (job: ParkGraphBulkExportJob): void => {
          this.handleExportJobUpdate(job);
          if (!this.isTerminalExportJob(job)) {
            this.startExportPolling(job.jobId);
          }
        },
        error: (error: unknown): void => {
          this.isExporting = false;
          this.uiError = 'admin.bulkParkGraphUpserts.errors.exportFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.bulkParkGraphUpserts.errors.exportFailed', 'Bulk JSON export failed.'));
          this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.exportFailedTitle', 'admin.bulkParkGraphUpserts.toasts.exportFailedDetail');
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  protected downloadPreparedBulkJson(): void {
    const job: ParkGraphBulkExportJob | null = this.exportJob;
    if (!job?.downloadUrl || this.isDownloadingExport) {
      return;
    }

    this.isDownloadingExport = true;
    this.uiError = null;
    this.operationErrorDetail = null;
    this.changeDetectorRef.markForCheck();

    this.bulkDownloadSubscription?.unsubscribe();
    this.bulkDownloadSubscription = this.parkGraphUpsertsApi.downloadBulkParkExport(job.downloadUrl)
      .pipe(finalize((): void => {
        this.isDownloadingExport = false;
        this.bulkDownloadSubscription = null;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (response: HttpResponse<Blob>): void => {
          if (!response.body) {
            this.uiError = 'admin.bulkParkGraphUpserts.errors.exportFailed';
            this.operationErrorDetail = this.translate('admin.bulkParkGraphUpserts.errors.exportFailed', 'Bulk JSON export failed.');
            return;
          }

          this.downloadBlob(response.body, this.resolveDownloadFileName(response, job.fileName ?? 'bulk-park-graph-export.json'));
        },
        error: (error: unknown): void => {
          this.uiError = 'admin.bulkParkGraphUpserts.errors.exportFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.bulkParkGraphUpserts.errors.exportFailed', 'Bulk JSON export failed.'));
          this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.exportFailedTitle', 'admin.bulkParkGraphUpserts.toasts.exportFailedDetail');
        }
      });
  }

  protected loadJsonFile(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    const file: File | null = input.files?.[0] ?? null;
    if (!file) {
      return;
    }

    const reader: FileReader = new FileReader();
    reader.onload = (): void => {
      this.jsonText = typeof reader.result === 'string' ? reader.result : '';
      this.previewResult = null;
      this.lastAppliedResult = null;
      this.uiError = null;
      this.operationErrorDetail = null;
      this.changeDetectorRef.markForCheck();
    };
    reader.onerror = (): void => {
      this.uiError = 'admin.bulkParkGraphUpserts.errors.fileReadFailed';
      this.operationErrorDetail = null;
      this.changeDetectorRef.markForCheck();
    };
    reader.readAsText(file);
    input.value = '';
  }

  protected updateJsonText(value: string): void {
    if (value === this.jsonText) {
      return;
    }

    this.jsonText = value;
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.uiError = null;
    this.operationErrorDetail = null;
  }

  protected preview(): void {
    if (!this.canPreview) {
      return;
    }

    const request: BulkParkGraphUpsertRequest | null = this.buildImportRequest();
    if (!request) {
      return;
    }

    const previewedJsonText: string = this.jsonText;
    this.isPreviewing = true;
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.operationErrorDetail = null;
    this.parkGraphUpsertsApi.previewBulk(request)
      .pipe(finalize((): void => {
        this.isPreviewing = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (result: BulkParkGraphUpsertResult): void => {
          if (this.jsonText !== previewedJsonText) {
            return;
          }

          this.previewResult = result;
          this.notifyPreviewResult(result);
        },
        error: (error: unknown): void => {
          this.uiError = 'admin.bulkParkGraphUpserts.errors.previewFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.bulkParkGraphUpserts.errors.previewFailed', 'Bulk preview failed.'));
          this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.previewFailedTitle', 'admin.bulkParkGraphUpserts.toasts.previewFailedDetail');
        }
      });
  }

  protected apply(): void {
    const request: BulkParkGraphUpsertRequest | null = this.buildImportRequest();
    if (!request) {
      return;
    }

    if (!this.previewResult?.canApply) {
      this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.applyBlockedTitle', 'admin.bulkParkGraphUpserts.toasts.applyBlockedDetail');
      return;
    }

    this.isApplying = true;
    this.lastAppliedResult = null;
    this.operationErrorDetail = null;
    this.parkGraphUpsertsApi.applyBulk(request)
      .pipe(finalize((): void => {
        this.isApplying = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (result: BulkParkGraphUpsertResult): void => {
          this.lastAppliedResult = result;
          this.previewResult = result;
          this.notifyApplyResult(result);
        },
        error: (error: unknown): void => {
          this.uiError = 'admin.bulkParkGraphUpserts.errors.applyFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.bulkParkGraphUpserts.errors.applyFailed', 'Bulk apply failed.'));
          this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.applyFailedTitle', 'admin.bulkParkGraphUpserts.toasts.applyFailedDetail');
        }
      });
  }

  protected get selectedCount(): number {
    return this.selectedParkIds.length;
  }

  protected get canClearSearch(): boolean {
    return this.searchQuery.trim().length > 0;
  }

  protected get canExport(): boolean {
    return !this.isExporting && (this.selectionMode === 'filtered' || this.selectedParkIds.length > 0);
  }

  protected get exportProgressPercentage(): number {
    return Math.max(0, Math.min(100, this.exportJob?.progressPercentage ?? (this.isExporting ? 1 : 0)));
  }

  protected get exportHasProgressCount(): boolean {
    return this.exportJob?.processedParkCount !== null
      && this.exportJob?.processedParkCount !== undefined
      && this.exportJob?.exportedParkCount !== null
      && this.exportJob?.exportedParkCount !== undefined;
  }

  protected get canPreview(): boolean {
    return this.jsonText.trim().length > 0 && !this.isPreviewing;
  }

  protected get canApply(): boolean {
    return Boolean(this.previewResult?.canApply) && !this.isApplying && !this.isPreviewing;
  }

  protected get sortOrder(): 1 | -1 {
    return this.sortDirection === 'desc' ? -1 : 1;
  }

  protected get sortValue(): AdminParkSortOptionValue {
    return `${this.sortField}:${this.sortDirection}` as AdminParkSortOptionValue;
  }

  protected get exportedScopeLabelKey(): string {
    return this.selectionMode === 'explicit'
      ? 'admin.bulkParkGraphUpserts.selection.explicitScope'
      : 'admin.bulkParkGraphUpserts.selection.filteredScope';
  }

  protected get appliedResultMessageKey(): string | null {
    const result: BulkParkGraphUpsertResult | null = this.lastAppliedResult;
    if (!result) {
      return null;
    }

    if (!this.hasResultFailureSignals(result)) {
      return 'admin.bulkParkGraphUpserts.result.applied';
    }

    return this.countAppliedMutations(result) > 0
      ? 'admin.bulkParkGraphUpserts.result.appliedPartial'
      : 'admin.bulkParkGraphUpserts.result.appliedNoChange';
  }

  protected get appliedResultMessageParams(): Record<string, number> {
    const result: BulkParkGraphUpsertResult | null = this.lastAppliedResult;
    if (!result) {
      return { applied: 0, failed: 0 };
    }

    return {
      applied: this.countAppliedMutations(result),
      failed: this.countRejectedEntries(result)
    };
  }

  protected get isAppliedResultSuccess(): boolean {
    return Boolean(this.lastAppliedResult) && !this.hasResultFailureSignals(this.lastAppliedResult);
  }

  protected get isAppliedResultPartial(): boolean {
    return Boolean(this.lastAppliedResult)
      && this.hasResultFailureSignals(this.lastAppliedResult)
      && this.countAppliedMutations(this.lastAppliedResult) > 0;
  }

  protected get isAppliedResultRejected(): boolean {
    return Boolean(this.lastAppliedResult)
      && this.hasResultFailureSignals(this.lastAppliedResult)
      && this.countAppliedMutations(this.lastAppliedResult) === 0;
  }

  protected get groupedErrors(): BulkParkGraphUpsertMessageGroup[] {
    return this.groupMessages(this.previewResult?.errors ?? []);
  }

  protected get groupedWarnings(): BulkParkGraphUpsertMessageGroup[] {
    return this.groupMessages(this.previewResult?.warnings ?? []);
  }

  protected get hasOperationErrors(): boolean {
    return this.collectOperationErrors().length > 0;
  }

  protected get hasJsonDraft(): boolean {
    return this.jsonText.trim().length > 0;
  }

  protected isParkSelected(park: Park): boolean {
    return !!park.id && this.selectedParkIds.includes(park.id);
  }

  protected areAllCurrentParksSelected(): boolean {
    const visibleIds: string[] = this.parks.map((park: Park): string | undefined => park.id).filter((parkId: string | undefined): parkId is string => !!parkId);
    return visibleIds.length > 0 && visibleIds.every((parkId: string): boolean => this.selectedParkIds.includes(parkId));
  }

  protected isSectionSelected(section: ParkGraphExportSection): boolean {
    return this.selectedSections.includes(section);
  }

  protected getTypeTranslationKey(type: string | null | undefined): string {
    return getParkTypeTranslationKey(type);
  }

  protected getAudienceClassificationTranslationKey(classification: ParkAudienceClassificationFilter | string | null | undefined): string {
    return getParkAudienceClassificationTranslationKey(classification);
  }

  protected getStatusSeverity(status: AdminReviewStatus | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
    return getAdminReviewStatusSeverity(status);
  }

  protected getStatusLabelKey(status: AdminReviewStatus | null | undefined): string {
    return getAdminReviewStatusTranslationKey(status);
  }

  protected hasValidCoordinates(park: Park): boolean {
    const latitude: number = Number(park.latitude);
    const longitude: number = Number(park.longitude);

    return Number.isFinite(latitude)
      && Number.isFinite(longitude)
      && (latitude !== 0 || longitude !== 0);
  }

  protected getCoordinatesLabel(park: Park): string {
    if (!this.hasValidCoordinates(park)) {
      return 'admin.parks.coordinatesMissing';
    }

    return `${park.latitude.toFixed(6)}, ${park.longitude.toFixed(6)}`;
  }

  protected getParkItemsTotalCountLabel(park: Park): string {
    return this.formatCount(park.parkItemsTotalCount);
  }

  protected getParkItemsVisibleCountLabel(park: Park): string {
    return this.formatCount(park.parkItemsVisibleCount);
  }

  protected getOpeningHoursStatusLabelKey(status: ParkOpeningHoursAdminStatus | null | undefined): string {
    return `admin.parks.openingHours.statuses.${status ?? 'NotConfigured'}`;
  }

  protected getOpeningHoursStatusSeverity(status: ParkOpeningHoursAdminStatus | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
    switch (status) {
      case 'UpToDate':
        return 'success';
      case 'NeedsUpdate':
        return 'warn';
      case 'Expired':
        return 'danger';
      case 'NotConfigured':
      default:
        return 'info';
    }
  }

  protected getOpeningHoursCoverageLabelKey(park: Park): string {
    if (!park.openingHours?.hasOpeningHours) {
      return 'admin.parks.openingHours.notConfigured';
    }

    return park.openingHours.completeForDays === 1
      ? 'admin.parks.openingHours.coverageOne'
      : 'admin.parks.openingHours.coverageMany';
  }

  protected getOpeningHoursCoverageParams(park: Park): Record<string, string | number> {
    return {
      count: park.openingHours?.completeForDays ?? 0,
      date: park.openingHours?.completeUntilDate ?? park.openingHours?.lastDate ?? '-'
    };
  }

  protected trackPark(_: number, park: Park): string {
    return park.id ?? park.name ?? `${park.latitude}-${park.longitude}`;
  }

  protected trackSection(_: number, option: ParkGraphExportSectionOption): ParkGraphExportSection {
    return option.value;
  }

  protected trackBulkParkResult(_: number, result: BulkParkGraphUpsertResult['parks'][number]): string {
    return `${result.index}:${result.targetParkId ?? result.targetParkName ?? 'park'}`;
  }

  protected trackMessageGroup(_: number, group: BulkParkGraphUpsertMessageGroup): string {
    return `${group.entityType}:${group.displayName}`;
  }

  protected trackString(_: number, value: string): string {
    return value;
  }

  protected async copyOperationErrors(): Promise<void> {
    const messages: string[] = this.collectOperationErrors();
    await this.copyMessages(messages, 'admin.bulkParkGraphUpserts.toasts.errorsCopiedTitle', 'admin.bulkParkGraphUpserts.toasts.errorsCopiedDetail');
  }

  protected async copyResultErrors(): Promise<void> {
    const messages: string[] = this.previewResult?.errors ?? [];
    await this.copyMessages(messages, 'admin.bulkParkGraphUpserts.toasts.errorsCopiedTitle', 'admin.bulkParkGraphUpserts.toasts.errorsCopiedDetail');
  }

  private loadParks(page: number, size: number): void {
    const requestId: number = ++this.listRequestId;
    const query: string = this.searchQuery.trim();
    const filters: ParkAdminListFilters = this.buildFilters();
    this.loading = true;
    this.uiError = null;
    this.operationErrorDetail = null;

    const request = query.length > 0
      ? this.parksApi.searchParks(query, page, size, false, null, filters, { sort: this.buildSort(), closedFilter: this.localClosedFilter })
      : this.parksApi.getParksPaginated(page, size, false, null, filters, { sort: this.buildSort(), closedFilter: this.localClosedFilter });

    request.pipe(finalize((): void => {
      if (requestId === this.listRequestId) {
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      }
    })).subscribe({
      next: (response: ParksApiResponse): void => {
        if (requestId !== this.listRequestId) {
          return;
        }

        this.parks = response.data ?? [];
        this.totalRecords = response.pagination?.totalItems ?? this.parks.length;
        this.currentPage = response.pagination?.currentPage ?? page;
        this.pageSize = response.pagination?.itemsPerPage ?? size;
      },
      error: (error: unknown): void => {
        if (requestId !== this.listRequestId) {
          return;
        }

        this.parks = [];
        this.totalRecords = 0;
        this.uiError = 'admin.bulkParkGraphUpserts.errors.loadParksFailed';
        this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.bulkParkGraphUpserts.errors.loadParksFailed', 'Park list loading failed.'));
      }
    });
  }

  private buildExportRequest(): ParkGraphBulkExportRequest {
    const filters: ParkAdminListFilters = this.buildFilters();
    return {
      selectionMode: this.selectionMode,
      parkIds: this.selectionMode === 'explicit' ? [...this.selectedParkIds] : [],
      searchTerm: this.searchQuery.trim() || null,
      isVisible: filters.isVisible ?? null,
      adminReviewStatus: filters.adminReviewStatus ?? null,
      type: filters.type ?? null,
      audienceClassification: filters.audienceClassification ?? null,
      countryCode: filters.countryCode ?? null,
      hasValidCoordinates: filters.hasValidCoordinates ?? null,
      closedFilter: this.localClosedFilter,
      openingHoursStatus: filters.openingHoursStatus ?? 'all',
      sortBy: this.sortField,
      sortDirection: this.sortDirection,
      sections: [...this.selectedSections]
    };
  }

  private buildImportRequest(): BulkParkGraphUpsertRequest | null {
    this.uiError = null;
    this.operationErrorDetail = null;

    let document: unknown;
    try {
      document = JSON.parse(this.jsonText);
    } catch {
      this.uiError = 'admin.bulkParkGraphUpserts.errors.invalidJson';
      this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.invalidJsonTitle', 'admin.bulkParkGraphUpserts.toasts.invalidJsonDetail');
      return null;
    }

    if (!this.isJsonObject(document)) {
      this.uiError = 'admin.bulkParkGraphUpserts.errors.invalidJson';
      this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.invalidJsonTitle', 'admin.bulkParkGraphUpserts.toasts.invalidJsonDetail');
      return null;
    }

    return {
      createIfMissing: false,
      replaceCollections: this.replaceCollections,
      document
    };
  }

  private buildFilters(): ParkAdminListFilters {
    const countryCode: string = this.localCountryCodeFilter.trim();
    return {
      isVisible: this.localVisibilityFilter,
      adminReviewStatus: this.localAdminReviewStatusFilter,
      type: this.localTypeFilter,
      audienceClassification: this.localAudienceClassificationFilter,
      countryCode: countryCode.length > 0 ? countryCode.toUpperCase() : null,
      hasValidCoordinates: this.localValidCoordinatesFilter,
      openingHoursStatus: this.localOpeningHoursFilter
    };
  }

  private buildSort(): { sortBy: ParkAdminListSortField; sortDirection: ParkAdminListSortDirection } {
    return {
      sortBy: this.sortField,
      sortDirection: this.sortDirection
    };
  }

  private startExportPolling(jobId: string): void {
    this.stopExportPolling();
    this.exportPollingSubscription = concat(of(0), interval(1000))
      .pipe(concatMap(() => this.parkGraphUpsertsApi.getBulkParkExportJob(jobId)))
      .subscribe({
        next: (job: ParkGraphBulkExportJob): void => this.handleExportJobUpdate(job),
        error: (error: unknown): void => {
          this.isExporting = false;
          this.stopExportPolling();
          this.uiError = 'admin.bulkParkGraphUpserts.errors.exportFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.bulkParkGraphUpserts.errors.exportFailed', 'Bulk JSON export failed.'));
          this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.exportFailedTitle', 'admin.bulkParkGraphUpserts.toasts.exportFailedDetail');
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  private handleExportJobUpdate(job: ParkGraphBulkExportJob): void {
    this.exportJob = job;
    if (job.status === 'Completed') {
      this.isExporting = false;
      this.stopExportPolling();
      if (!job.downloadUrl) {
        this.uiError = 'admin.bulkParkGraphUpserts.errors.exportFailed';
        this.operationErrorDetail = this.translate('admin.bulkParkGraphUpserts.errors.exportFailed', 'Bulk JSON export failed.');
        this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.exportFailedTitle', 'admin.bulkParkGraphUpserts.toasts.exportFailedDetail');
      } else {
        this.showToast('success', 'admin.bulkParkGraphUpserts.toasts.exportSuccessTitle', 'admin.bulkParkGraphUpserts.toasts.exportSuccessDetail');
      }

      this.changeDetectorRef.markForCheck();
      return;
    }

    if (job.status === 'Failed' || job.status === 'Expired') {
      this.isExporting = false;
      this.stopExportPolling();
      this.uiError = 'admin.bulkParkGraphUpserts.errors.exportFailed';
      this.operationErrorDetail = job.error ?? job.message ?? this.translate('admin.bulkParkGraphUpserts.errors.exportFailed', 'Bulk JSON export failed.');
      this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.exportFailedTitle', 'admin.bulkParkGraphUpserts.toasts.exportFailedDetail');
      this.changeDetectorRef.markForCheck();
      return;
    }

    this.changeDetectorRef.markForCheck();
  }

  private isTerminalExportJob(job: ParkGraphBulkExportJob): boolean {
    return job.status === 'Completed' || job.status === 'Failed' || job.status === 'Expired';
  }

  private stopExportPolling(): void {
    this.exportPollingSubscription?.unsubscribe();
    this.exportPollingSubscription = null;
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    if (!this.document.defaultView || typeof URL === 'undefined' || typeof URL.createObjectURL !== 'function') {
      this.uiError = 'admin.bulkParkGraphUpserts.errors.exportFailed';
      this.operationErrorDetail = this.translate('admin.bulkParkGraphUpserts.errors.exportFailed', 'Bulk JSON export failed.');
      return;
    }

    const objectUrl: string = URL.createObjectURL(blob);
    const anchor: HTMLAnchorElement = this.document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.rel = 'noopener';
    this.document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(objectUrl);
  }

  private resolveDownloadFileName(response: HttpResponse<Blob>, fallbackFileName: string): string {
    const contentDisposition: string = response.headers.get('content-disposition') ?? '';
    const utf8Match: RegExpMatchArray | null = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (utf8Match?.[1]) {
      return decodeURIComponent(utf8Match[1].replace(/"/g, ''));
    }

    const fallbackMatch: RegExpMatchArray | null = contentDisposition.match(/filename="?([^";]+)"?/i);
    if (fallbackMatch?.[1]) {
      return fallbackMatch[1];
    }

    return fallbackFileName;
  }

  private updateSort(sortField: ParkAdminListSortField, sortDirection: ParkAdminListSortDirection): boolean {
    const sortChanged: boolean = this.sortField !== sortField || this.sortDirection !== sortDirection;
    this.sortField = sortField;
    this.sortDirection = sortDirection;
    return sortChanged;
  }

  private normalizeSortField(sortField: string | string[] | null | undefined): ParkAdminListSortField {
    const primarySortField: string | null | undefined = Array.isArray(sortField) ? sortField[0] : sortField;

    switch (primarySortField) {
      case 'name':
        return 'name';
      case 'parkItemsTotalCount':
        return 'parkItemsTotalCount';
      case 'parkItemsVisibleCount':
        return 'parkItemsVisibleCount';
      case 'openingHoursStatus':
        return 'openingHoursStatus';
      default:
        return 'default';
    }
  }

  private normalizeSortDirection(sortOrder: number | null | undefined): ParkAdminListSortDirection {
    return sortOrder === -1 ? 'desc' : 'asc';
  }

  private notifyPreviewResult(result: BulkParkGraphUpsertResult): void {
    if (result.errors.length > 0 || !result.canApply) {
      this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.previewBlockedTitle', 'admin.bulkParkGraphUpserts.toasts.previewBlockedDetail', { count: result.errors.length });
      return;
    }

    if (result.warnings.length > 0 || result.parks.some(park => park.result.changes.some((change: ParkGraphUpsertChange): boolean => change.changeType === 'Skipped'))) {
      this.showToast('warn', 'admin.bulkParkGraphUpserts.toasts.previewPartialTitle', 'admin.bulkParkGraphUpserts.toasts.previewPartialDetail');
      return;
    }

    this.showToast('success', 'admin.bulkParkGraphUpserts.toasts.previewSuccessTitle', 'admin.bulkParkGraphUpserts.toasts.previewSuccessDetail');
  }

  private notifyApplyResult(result: BulkParkGraphUpsertResult): void {
    if (this.hasResultFailureSignals(result) && this.countAppliedMutations(result) > 0) {
      this.showToast('warn', 'admin.bulkParkGraphUpserts.toasts.applyPartialTitle', 'admin.bulkParkGraphUpserts.toasts.applyPartialDetail');
      return;
    }

    if (this.hasResultFailureSignals(result)) {
      const severity: ToastSeverity = result.errors.length > 0 ? 'error' : 'warn';
      this.showToast(severity, 'admin.bulkParkGraphUpserts.toasts.applyRejectedTitle', 'admin.bulkParkGraphUpserts.toasts.applyRejectedDetail', { count: this.countRejectedEntries(result) });
      return;
    }

    this.showToast('success', 'admin.bulkParkGraphUpserts.toasts.applySuccessTitle', 'admin.bulkParkGraphUpserts.toasts.applySuccessDetail');
  }

  private hasResultFailureSignals(result: BulkParkGraphUpsertResult | null): boolean {
    return Boolean(result && (result.errors.length > 0 || result.parks.some(park => park.result.changes.some((change: ParkGraphUpsertChange): boolean => change.changeType === 'Skipped'))));
  }

  private countAppliedMutations(result: BulkParkGraphUpsertResult | null): number {
    if (!result) {
      return 0;
    }

    return result.counts.created + result.counts.updated + result.counts.deleted;
  }

  private countRejectedEntries(result: BulkParkGraphUpsertResult | null): number {
    if (!result) {
      return 0;
    }

    const skippedChanges: number = result.parks.reduce((count: number, parkResult: BulkParkGraphUpsertResult['parks'][number]): number => {
      return count + parkResult.result.changes.filter((change: ParkGraphUpsertChange): boolean => change.changeType === 'Skipped').length;
    }, 0);
    return skippedChanges > 0 ? skippedChanges : result.errors.length;
  }

  private groupMessages(messages: string[]): BulkParkGraphUpsertMessageGroup[] {
    const groups = new Map<string, BulkParkGraphUpsertMessageGroup>();

    for (const message of messages) {
      const change: ParkGraphUpsertChange | undefined = this.findRelatedChange(message);
      const entityType: string = change?.entityType ?? 'Document';
      const displayName: string = change?.displayName ?? 'Bulk';
      const key: string = `${entityType}:${displayName}`;
      const group: BulkParkGraphUpsertMessageGroup = groups.get(key) ?? {
        entityType,
        displayName,
        messages: []
      };

      group.messages.push(message);
      groups.set(key, group);
    }

    return Array.from(groups.values());
  }

  private findRelatedChange(message: string): ParkGraphUpsertChange | undefined {
    const normalizedMessage: string = message.toLocaleLowerCase();
    const changes: ParkGraphUpsertChange[] = this.previewResult?.parks.flatMap(park => park.result.changes) ?? [];
    return changes.find((change: ParkGraphUpsertChange): boolean => {
      const displayName: string = change.displayName.toLocaleLowerCase();
      const entityKey: string = (change.entityKey ?? '').toLocaleLowerCase();
      const entityType: string = change.entityType.toLocaleLowerCase();
      return (displayName.length > 0 && normalizedMessage.includes(displayName))
        || (entityKey.length > 0 && normalizedMessage.includes(entityKey))
        || normalizedMessage.includes(entityType);
    });
  }

  private collectOperationErrors(): string[] {
    const messages: string[] = [];
    if (this.uiError) {
      messages.push(this.translate(this.uiError, this.uiError));
    }

    if (this.operationErrorDetail) {
      messages.push(this.operationErrorDetail);
    }

    return messages;
  }

  private async copyMessages(messages: string[], summaryKey: string, detailKey: string): Promise<void> {
    if (messages.length === 0) {
      return;
    }

    try {
      await this.writeToClipboard(messages.join('\n'));
      this.showToast('success', summaryKey, detailKey);
    } catch {
      this.showToast('error', 'admin.bulkParkGraphUpserts.toasts.copyFailedTitle', 'admin.bulkParkGraphUpserts.toasts.copyFailedDetail');
    }
  }

  private async writeToClipboard(value: string): Promise<void> {
    const defaultView: Window | null = this.document.defaultView;
    if (defaultView?.navigator.clipboard?.writeText) {
      await defaultView.navigator.clipboard.writeText(value);
      return;
    }

    const textArea: HTMLTextAreaElement = this.document.createElement('textarea');
    textArea.value = value;
    textArea.setAttribute('readonly', 'true');
    textArea.style.position = 'fixed';
    textArea.style.left = '-9999px';
    this.document.body.appendChild(textArea);
    textArea.select();
    this.document.execCommand('copy');
    this.document.body.removeChild(textArea);
  }

  private showToast(severity: ToastSeverity, summaryKey: string, detailKey: string, params: Record<string, number | string> = {}): void {
    this.toastMessageService.add(
      severity,
      this.translate(summaryKey, summaryKey, params),
      this.translate(detailKey, detailKey, params));
  }

  private translate(key: string, fallback: string, params: Record<string, number | string> = {}): string {
    const translatedValue: string = this.translateService.instant(key, params);
    return translatedValue === key ? fallback : translatedValue;
  }

  private formatCount(count: number | null | undefined): string {
    return count === null || count === undefined ? '-' : String(count);
  }

  private isJsonObject(value: unknown): value is JsonObject {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
  }

  protected countParkChanges(counts: ParkGraphUpsertCounts): number {
    return counts.created + counts.updated + counts.deleted;
  }
}
