import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, HostListener, Inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkGraphUpsertChange, ParkGraphUpsertCounts, ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { extractSafeDisplayErrorMessage } from '@shared/utils/security';

type ParkGraphUpsertChangeTypeFilter = 'All' | 'Created' | 'Updated' | 'Deleted' | 'Unchanged' | 'Warning' | 'Skipped';
type ToastSeverity = 'success' | 'info' | 'warn' | 'error';
type JsonObject = Record<string, unknown>;

interface ParkGraphUpsertMessageGroup {
  entityType: string;
  displayName: string;
  messages: string[];
}

interface ParkGraphUpsertSourceLocation {
  path: readonly string[];
  idFields: readonly string[];
  nameFields: readonly string[];
}

@Component({
  selector: 'app-admin-park-graph-upserts',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, ImageDisplayComponent, SafeExternalUrlPipe],
  templateUrl: './admin-park-graph-upserts.component.html',
  styleUrl: './admin-park-graph-upserts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminParkGraphUpsertsComponent implements OnInit {
  private static readonly removableSourceLocations: Record<string, ParkGraphUpsertSourceLocation> = {
    ParkZone: {
      path: ['zones'],
      idFields: ['id', 'zoneId', 'parkZoneId'],
      nameFields: ['name', 'slug']
    },
    ParkItem: {
      path: ['items'],
      idFields: ['id', 'itemId', 'parkItemId'],
      nameFields: ['name']
    },
    Image: {
      path: ['images'],
      idFields: ['id', 'imageId'],
      nameFields: ['description', 'sourceUrl', 'remoteUrl', 'externalUrl']
    },
    ParkFounder: {
      path: ['references', 'founders'],
      idFields: ['id'],
      nameFields: ['name']
    },
    ParkOperator: {
      path: ['references', 'operators'],
      idFields: ['id'],
      nameFields: ['name', 'legalName']
    },
    AttractionManufacturer: {
      path: ['references', 'manufacturers'],
      idFields: ['id'],
      nameFields: ['name', 'legalName']
    }
  };

  protected readonly changeTypeFilters: ParkGraphUpsertChangeTypeFilter[] = ['All', 'Created', 'Updated', 'Deleted', 'Unchanged', 'Warning', 'Skipped'];
  protected readonly expertJsonPlaceholder: string = this.buildDefaultJson();

  protected searchTerm: string = '';
  protected searchResults: Park[] = [];
  protected selectedPark: Park | null = null;
  protected jsonText: string = '';
  protected previewResult: ParkGraphUpsertResult | null = null;
  protected lastAppliedResult: ParkGraphUpsertResult | null = null;
  protected changeTypeFilter: ParkGraphUpsertChangeTypeFilter = 'All';
  protected entityTypeFilter: string = 'All';
  protected isSearching: boolean = false;
  protected isPreviewing: boolean = false;
  protected isApplying: boolean = false;
  protected isExporting: boolean = false;
  protected uiError: string | null = null;
  protected operationErrorDetail: string | null = null;
  protected isNearPageBottom: boolean = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly parksApi: ParksApiService,
    private readonly parkGraphUpsertsApi: ParkGraphUpsertsApiService,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
  }

  ngOnInit(): void {
    this.selectParkFromQueryParams(this.route.snapshot.queryParamMap);
    this.updatePageScrollState();
  }

  @HostListener('window:scroll')
  protected onWindowScroll(): void {
    this.updatePageScrollState();
  }

  protected searchParks(): void {
    const query: string = this.searchTerm.trim();
    this.uiError = null;
    this.operationErrorDetail = null;
    this.searchResults = [];
    if (query.length < 2) {
      this.uiError = 'admin.parkGraphUpserts.errors.searchTooShort';
      this.changeDetectorRef.markForCheck();
      return;
    }

    this.isSearching = true;
    this.parksApi.searchParks(query, 1, 10, false, null, null)
      .pipe(finalize((): void => {
        this.isSearching = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (response: ParksApiResponse): void => {
          this.searchResults = response.data ?? [];
        },
        error: (error: unknown): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.searchFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.parkGraphUpserts.errors.searchFailed', 'Park search failed.'));
          this.showToast('error', 'admin.parkGraphUpserts.toasts.searchFailedTitle', 'admin.parkGraphUpserts.toasts.searchFailedDetail');
        }
      });
  }

  protected selectPark(park: Park): void {
    this.selectedPark = park;
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.uiError = null;
    this.operationErrorDetail = null;
  }

  protected clearSelectedPark(): void {
    this.selectedPark = null;
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.operationErrorDetail = null;
  }

  protected exportSelectedParkJson(): void {
    this.uiError = null;
    this.operationErrorDetail = null;

    if (!this.selectedPark?.id) {
      this.uiError = 'admin.parkGraphUpserts.errors.noParkSelected';
      this.changeDetectorRef.markForCheck();
      return;
    }

    this.isExporting = true;
    this.parkGraphUpsertsApi.downloadParkExport(this.selectedPark.id)
      .pipe(finalize((): void => {
        this.isExporting = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (response: HttpResponse<Blob>): void => {
          if (!response.body) {
            this.uiError = 'admin.parkGraphUpserts.errors.exportFailed';
            return;
          }

          this.downloadBlob(response.body, this.resolveDownloadFileName(response));
        },
        error: (error: unknown): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.exportFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.parkGraphUpserts.errors.exportFailed', 'JSON export failed.'));
        }
      });
  }

  protected loadExpertJsonFile(event: Event): void {
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
      this.uiError = 'admin.parkGraphUpserts.errors.fileReadFailed';
      this.operationErrorDetail = null;
      this.changeDetectorRef.markForCheck();
    };
    reader.readAsText(file);
    input.value = '';
  }

  protected preview(): void {
    const request: ParkGraphUpsertRequest | null = this.buildRequest();
    if (!request) {
      return;
    }

    this.isPreviewing = true;
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.operationErrorDetail = null;
    this.parkGraphUpsertsApi.preview(request)
      .pipe(finalize((): void => {
        this.isPreviewing = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (result: ParkGraphUpsertResult): void => {
          this.previewResult = result;
          this.notifyPreviewResult(result);
        },
        error: (error: unknown): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.previewFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.parkGraphUpserts.errors.previewFailed', 'Preview failed.'));
          this.showToast('error', 'admin.parkGraphUpserts.toasts.previewFailedTitle', 'admin.parkGraphUpserts.toasts.previewFailedDetail');
        }
      });
  }

  protected apply(): void {
    const request: ParkGraphUpsertRequest | null = this.buildRequest();
    if (!request) {
      return;
    }

    if (!this.previewResult?.canApply) {
      this.showToast('error', 'admin.parkGraphUpserts.toasts.applyBlockedTitle', 'admin.parkGraphUpserts.toasts.applyBlockedDetail');
      return;
    }

    this.isApplying = true;
    this.lastAppliedResult = null;
    this.operationErrorDetail = null;
    this.parkGraphUpsertsApi.apply(request)
      .pipe(finalize((): void => {
        this.isApplying = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (result: ParkGraphUpsertResult): void => {
          this.lastAppliedResult = result;
          this.previewResult = result;
          this.notifyApplyResult(result);
        },
        error: (error: unknown): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.applyFailed';
          this.operationErrorDetail = extractSafeDisplayErrorMessage(error, this.translate('admin.parkGraphUpserts.errors.applyFailed', 'Apply failed.'));
          this.showToast('error', 'admin.parkGraphUpserts.toasts.applyFailedTitle', 'admin.parkGraphUpserts.toasts.applyFailedDetail');
        }
      });
  }

  protected removePreviewBlock(change: ParkGraphUpsertChange): void {
    const document: JsonObject | null = this.parseJsonDraftForEdit();
    if (document === null) {
      return;
    }

    const deletionQueued: boolean = this.queueDeletionIfSupported(document, change);
    const sourceRemoved: boolean = this.removeSourceBlock(document, change);
    if (!deletionQueued && !sourceRemoved) {
      this.showToast('error', 'admin.parkGraphUpserts.toasts.blockRemoveFailedTitle', 'admin.parkGraphUpserts.toasts.blockRemoveFailedDetail');
      return;
    }

    this.jsonText = JSON.stringify(document, null, 2);
    this.lastAppliedResult = null;
    this.removeChangeFromPreview(change);
    this.showToast('success', 'admin.parkGraphUpserts.toasts.blockRemovedTitle', deletionQueued ? 'admin.parkGraphUpserts.toasts.blockQueuedForDeletionDetail' : 'admin.parkGraphUpserts.toasts.blockRemovedDetail');
    this.changeDetectorRef.markForCheck();
  }

  protected async copyOperationErrors(): Promise<void> {
    const messages: string[] = this.collectOperationErrors();
    await this.copyMessages(messages, 'admin.parkGraphUpserts.toasts.errorsCopiedTitle', 'admin.parkGraphUpserts.toasts.errorsCopiedDetail');
  }

  protected async copyResultErrors(): Promise<void> {
    const messages: string[] = this.previewResult?.errors ?? [];
    await this.copyMessages(messages, 'admin.parkGraphUpserts.toasts.errorsCopiedTitle', 'admin.parkGraphUpserts.toasts.errorsCopiedDetail');
  }

  protected togglePageScroll(): void {
    const defaultView: Window | null = this.document.defaultView;
    if (!defaultView) {
      return;
    }

    const documentElement: HTMLElement = this.document.documentElement;
    const targetTop: number = this.isNearPageBottom ? 0 : documentElement.scrollHeight;
    defaultView.scrollTo({
      top: targetTop,
      behavior: 'smooth'
    });
  }

  protected get changes(): ParkGraphUpsertChange[] {
    return this.previewResult?.changes ?? [];
  }

  protected get filteredChanges(): ParkGraphUpsertChange[] {
    return this.changes.filter((change: ParkGraphUpsertChange): boolean => {
      const changeTypeMatches: boolean = this.changeTypeFilter === 'All' || change.changeType === this.changeTypeFilter;
      const entityTypeMatches: boolean = this.entityTypeFilter === 'All' || change.entityType === this.entityTypeFilter;
      return changeTypeMatches && entityTypeMatches;
    });
  }

  protected get entityTypeOptions(): string[] {
    return Array.from(new Set(this.changes.map((change: ParkGraphUpsertChange): string => change.entityType))).sort();
  }

  protected get contentChangeCount(): number {
    return this.changes.reduce((count: number, change: ParkGraphUpsertChange): number => {
      return count + change.fields.filter(field => this.isContentField(field.field)).length;
    }, 0);
  }

  protected get groupedErrors(): ParkGraphUpsertMessageGroup[] {
    return this.groupMessages(this.previewResult?.errors ?? []);
  }

  protected get groupedWarnings(): ParkGraphUpsertMessageGroup[] {
    return this.groupMessages(this.previewResult?.warnings ?? []);
  }

  protected get canApply(): boolean {
    return Boolean(this.previewResult?.canApply) && !this.isApplying && !this.isPreviewing;
  }

  protected get hasOperationErrors(): boolean {
    return this.collectOperationErrors().length > 0;
  }

  protected get hasJsonDraft(): boolean {
    return this.jsonText.trim().length > 0;
  }

  protected trackPark(_: number, park: Park): string {
    return park.id ?? park.name ?? `${park.latitude}-${park.longitude}`;
  }

  protected trackChange(index: number, change: ParkGraphUpsertChange): string {
    return `${change.entityType}-${change.entityId ?? change.entityKey ?? index}`;
  }

  protected trackMessageGroup(_: number, group: ParkGraphUpsertMessageGroup): string {
    return `${group.entityType}-${group.displayName}`;
  }

  protected trackString(_: number, value: string): string {
    return value;
  }

  protected resolveChangePreviewImageUrl(change: ParkGraphUpsertChange): string | null {
    if (change.entityType !== 'Image') {
      return null;
    }

    return this.findFieldValue(change, 'internalUrl') ?? this.findFieldValue(change, 'sourceUrl');
  }

  protected resolveChangeSourceUrl(change: ParkGraphUpsertChange): string | null {
    if (change.entityType !== 'Image') {
      return null;
    }

    return this.findFieldValue(change, 'sourceUrl');
  }

  protected canRemovePreviewBlock(change: ParkGraphUpsertChange): boolean {
    return change.entityType === 'Park'
      || Boolean(AdminParkGraphUpsertsComponent.removableSourceLocations[change.entityType])
      || this.isDeletionSupported(change);
  }

  private selectParkFromQueryParams(params: ParamMap): void {
    const parkId: string = params.get('parkId')?.trim() ?? '';
    if (!parkId) {
      return;
    }

    const latitude: number = Number(params.get('parkLatitude'));
    const longitude: number = Number(params.get('parkLongitude'));
    const name: string = params.get('parkName')?.trim() || parkId;

    this.selectedPark = {
      id: parkId,
      name,
      countryCode: params.get('parkCountryCode')?.trim() ?? '',
      city: params.get('parkCity')?.trim() ?? '',
      latitude: Number.isFinite(latitude) ? latitude : 0,
      longitude: Number.isFinite(longitude) ? longitude : 0
    };
    this.searchTerm = name;
  }

  private buildRequest(): ParkGraphUpsertRequest | null {
    this.uiError = null;
    this.operationErrorDetail = null;

    if (!this.selectedPark?.id) {
      this.uiError = 'admin.parkGraphUpserts.errors.noParkSelected';
      this.changeDetectorRef.markForCheck();
      return null;
    }

    let document: unknown;
    try {
      document = JSON.parse(this.jsonText);
    } catch {
      this.uiError = 'admin.parkGraphUpserts.errors.invalidJson';
      this.changeDetectorRef.markForCheck();
      return null;
    }

    return {
      targetParkId: this.selectedPark.id,
      createIfMissing: false,
      replaceCollections: false,
      document
    };
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    if (!this.document.defaultView || typeof URL === 'undefined') {
      this.uiError = 'admin.parkGraphUpserts.errors.exportFailed';
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

  private resolveDownloadFileName(response: HttpResponse<Blob>): string {
    const contentDisposition: string = response.headers.get('content-disposition') ?? '';
    const utf8Match: RegExpMatchArray | null = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (utf8Match?.[1]) {
      return decodeURIComponent(utf8Match[1].replace(/"/g, ''));
    }

    const fallbackMatch: RegExpMatchArray | null = contentDisposition.match(/filename="?([^";]+)"?/i);
    if (fallbackMatch?.[1]) {
      return fallbackMatch[1];
    }

    return 'park-graph-export.json';
  }

  private groupMessages(messages: string[]): ParkGraphUpsertMessageGroup[] {
    const groups = new Map<string, ParkGraphUpsertMessageGroup>();

    for (const message of messages) {
      const change: ParkGraphUpsertChange | undefined = this.findRelatedChange(message);
      const entityType: string = change?.entityType ?? 'Document';
      const displayName: string = change?.displayName ?? 'Graph';
      const key: string = `${entityType}:${displayName}`;
      const group: ParkGraphUpsertMessageGroup = groups.get(key) ?? {
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
    return this.changes.find((change: ParkGraphUpsertChange): boolean => {
      const displayName: string = change.displayName.toLocaleLowerCase();
      const entityKey: string = (change.entityKey ?? '').toLocaleLowerCase();
      const entityType: string = change.entityType.toLocaleLowerCase();
      return (displayName.length > 0 && normalizedMessage.includes(displayName))
        || (entityKey.length > 0 && normalizedMessage.includes(entityKey))
        || normalizedMessage.includes(entityType);
    });
  }

  private parseJsonDraftForEdit(): JsonObject | null {
    this.uiError = null;
    this.operationErrorDetail = null;

    let document: unknown;
    try {
      document = JSON.parse(this.jsonText);
    } catch {
      this.uiError = 'admin.parkGraphUpserts.errors.invalidJson';
      this.showToast('error', 'admin.parkGraphUpserts.toasts.invalidJsonTitle', 'admin.parkGraphUpserts.toasts.invalidJsonDetail');
      this.changeDetectorRef.markForCheck();
      return null;
    }

    if (!this.isJsonObject(document)) {
      this.uiError = 'admin.parkGraphUpserts.errors.invalidJson';
      this.showToast('error', 'admin.parkGraphUpserts.toasts.invalidJsonTitle', 'admin.parkGraphUpserts.toasts.invalidJsonDetail');
      this.changeDetectorRef.markForCheck();
      return null;
    }

    return document;
  }

  private queueDeletionIfSupported(document: JsonObject, change: ParkGraphUpsertChange): boolean {
    if (!this.isDeletionSupported(change) || !change.entityId) {
      return false;
    }

    const id: string = change.entityId.trim();
    if (id.length === 0 || this.isDeletionAlreadyQueued(document, change.entityType, id)) {
      return false;
    }

    const suppr: unknown = document['suppr'];
    if (Array.isArray(suppr)) {
      suppr.push({
        entityType: change.entityType,
        id
      });
      return true;
    }

    if (this.isJsonObject(suppr)) {
      const propertyName: string = this.resolveDeletionArrayProperty(change.entityType);
      const values: unknown = suppr[propertyName];
      if (Array.isArray(values)) {
        values.push(id);
      } else {
        suppr[propertyName] = [id];
      }

      return true;
    }

    document['suppr'] = [
      {
        entityType: change.entityType,
        id
      }
    ];
    return true;
  }

  private removeSourceBlock(document: JsonObject, change: ParkGraphUpsertChange): boolean {
    if (change.entityType === 'Park') {
      if (this.isJsonObject(document['park'])) {
        delete document['park'];
        return true;
      }

      return false;
    }

    const location: ParkGraphUpsertSourceLocation | undefined = AdminParkGraphUpsertsComponent.removableSourceLocations[change.entityType];
    if (!location) {
      return false;
    }

    const container: JsonObject | null = this.resolveContainer(document, location.path.slice(0, -1));
    const arrayPropertyName: string | undefined = location.path[location.path.length - 1];
    if (!container || !arrayPropertyName || !Array.isArray(container[arrayPropertyName])) {
      return false;
    }

    const values: unknown[] = container[arrayPropertyName] as unknown[];
    const index: number = values.findIndex((value: unknown): boolean => this.isMatchingSourceBlock(value, change, location));
    if (index < 0) {
      return false;
    }

    values.splice(index, 1);
    return true;
  }

  private removeChangeFromPreview(change: ParkGraphUpsertChange): void {
    if (!this.previewResult) {
      return;
    }

    const changes: ParkGraphUpsertChange[] = this.previewResult.changes.filter((candidate: ParkGraphUpsertChange): boolean => candidate !== change);
    this.previewResult = {
      ...this.previewResult,
      changes,
      counts: this.recountChanges(changes, this.previewResult.warnings.length, this.previewResult.errors.length)
    };
  }

  private recountChanges(changes: ParkGraphUpsertChange[], warnings: number, errors: number): ParkGraphUpsertCounts {
    return {
      created: changes.filter((change: ParkGraphUpsertChange): boolean => change.changeType === 'Created').length,
      updated: changes.filter((change: ParkGraphUpsertChange): boolean => change.changeType === 'Updated').length,
      deleted: changes.filter((change: ParkGraphUpsertChange): boolean => change.changeType === 'Deleted').length,
      unchanged: changes.filter((change: ParkGraphUpsertChange): boolean => change.changeType === 'Unchanged').length,
      warnings,
      errors
    };
  }

  private isMatchingSourceBlock(value: unknown, change: ParkGraphUpsertChange, location: ParkGraphUpsertSourceLocation): boolean {
    if (!this.isJsonObject(value)) {
      return false;
    }

    const id: string = change.entityId?.trim() ?? '';
    if (id.length > 0 && location.idFields.some((field: string): boolean => this.readNestedString(value, field) === id)) {
      return true;
    }

    const key: string = change.entityKey?.trim() ?? '';
    if (key.length > 0 && this.readNestedString(value, 'key') === key) {
      return true;
    }

    const displayName: string = this.normalizeComparableText(change.displayName);
    if (displayName.length === 0) {
      return false;
    }

    return location.nameFields.some((field: string): boolean => this.normalizeComparableText(this.readNestedString(value, field)) === displayName)
      || this.normalizeComparableText(this.readNestedString(value, 'identity.name')) === displayName;
  }

  private resolveContainer(document: JsonObject, path: readonly string[]): JsonObject | null {
    let current: unknown = document;
    for (const segment of path) {
      if (!this.isJsonObject(current)) {
        return null;
      }

      current = current[segment];
    }

    return this.isJsonObject(current) ? current : null;
  }

  private readNestedString(value: JsonObject, path: string): string | null {
    const segments: string[] = path.split('.');
    let current: unknown = value;
    for (const segment of segments) {
      if (!this.isJsonObject(current)) {
        return null;
      }

      current = current[segment];
    }

    return typeof current === 'string' ? current.trim() : null;
  }

  private isDeletionSupported(change: ParkGraphUpsertChange): boolean {
    return change.entityType === 'Image' || change.entityType === 'ParkItem' || change.entityType === 'ParkZone';
  }

  private resolveDeletionArrayProperty(entityType: string): string {
    if (entityType === 'Image') {
      return 'imageIds';
    }

    if (entityType === 'ParkItem') {
      return 'parkItemIds';
    }

    return 'parkZoneIds';
  }

  private isDeletionAlreadyQueued(document: JsonObject, entityType: string, id: string): boolean {
    const suppr: unknown = document['suppr'];
    if (Array.isArray(suppr)) {
      return suppr.some((entry: unknown): boolean => {
        if (typeof entry === 'string') {
          return entry.trim() === id;
        }

        if (!this.isJsonObject(entry)) {
          return false;
        }

        const entryId: string | null = this.readNestedString(entry, 'id')
          ?? this.readNestedString(entry, 'imageId')
          ?? this.readNestedString(entry, 'parkItemId')
          ?? this.readNestedString(entry, 'parkZoneId');
        return entryId === id;
      });
    }

    if (this.isJsonObject(suppr)) {
      const values: unknown = suppr[this.resolveDeletionArrayProperty(entityType)];
      return Array.isArray(values) && values.some((value: unknown): boolean => typeof value === 'string' && value.trim() === id);
    }

    return false;
  }

  private notifyPreviewResult(result: ParkGraphUpsertResult): void {
    if (result.errors.length > 0 || !result.canApply) {
      this.showToast('error', 'admin.parkGraphUpserts.toasts.previewBlockedTitle', 'admin.parkGraphUpserts.toasts.previewBlockedDetail', { count: result.errors.length });
      return;
    }

    if (result.warnings.length > 0 || result.changes.some((change: ParkGraphUpsertChange): boolean => change.changeType === 'Skipped')) {
      this.showToast('warn', 'admin.parkGraphUpserts.toasts.previewPartialTitle', 'admin.parkGraphUpserts.toasts.previewPartialDetail');
      return;
    }

    this.showToast('success', 'admin.parkGraphUpserts.toasts.previewSuccessTitle', 'admin.parkGraphUpserts.toasts.previewSuccessDetail');
  }

  private notifyApplyResult(result: ParkGraphUpsertResult): void {
    if (result.errors.length > 0 || result.changes.some((change: ParkGraphUpsertChange): boolean => change.changeType === 'Skipped')) {
      this.showToast('warn', 'admin.parkGraphUpserts.toasts.applyPartialTitle', 'admin.parkGraphUpserts.toasts.applyPartialDetail');
      return;
    }

    this.showToast('success', 'admin.parkGraphUpserts.toasts.applySuccessTitle', 'admin.parkGraphUpserts.toasts.applySuccessDetail');
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
      this.showToast('error', 'admin.parkGraphUpserts.toasts.copyFailedTitle', 'admin.parkGraphUpserts.toasts.copyFailedDetail');
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

  private updatePageScrollState(): void {
    const defaultView: Window | null = this.document.defaultView;
    if (!defaultView) {
      return;
    }

    const documentElement: HTMLElement = this.document.documentElement;
    const viewportBottom: number = defaultView.scrollY + defaultView.innerHeight;
    const nextIsNearBottom: boolean = documentElement.scrollHeight - viewportBottom < 140;
    if (nextIsNearBottom !== this.isNearPageBottom) {
      this.isNearPageBottom = nextIsNearBottom;
      this.changeDetectorRef.markForCheck();
    }
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

  private isJsonObject(value: unknown): value is JsonObject {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
  }

  private normalizeComparableText(value: string | null | undefined): string {
    return value?.trim().toLocaleLowerCase() ?? '';
  }

  private findFieldValue(change: ParkGraphUpsertChange, fieldName: string): string | null {
    const value: string | null | undefined = change.fields.find(field => field.field === fieldName)?.newValue;
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private isContentField(fieldName: string): boolean {
    const normalizedFieldName: string = fieldName.trim().toLocaleLowerCase();
    return normalizedFieldName === 'description'
      || normalizedFieldName.startsWith('description.')
      || normalizedFieldName.startsWith('descriptions.')
      || normalizedFieldName.startsWith('names.')
      || normalizedFieldName.startsWith('biography.')
      || normalizedFieldName.startsWith('alttexts.')
      || normalizedFieldName.startsWith('captions.')
      || normalizedFieldName.startsWith('credits.')
      || normalizedFieldName === 'attractiondetails.accessconditions';
  }

  private buildDefaultJson(): string {
    return JSON.stringify({
      documentType: 'AmusementParkParkGraphUpsert',
      schemaVersion: '2026-05-25',
      mode: 'merge',
      identity: {
        name: '',
        countryCode: ''
      },
      references: {
        operators: [],
        founders: [],
        manufacturers: []
      },
      park: {},
      zones: [],
      items: [],
      images: [],
      metadata: {
        source: 'manual-json'
      }
    }, null, 2);
  }
}
