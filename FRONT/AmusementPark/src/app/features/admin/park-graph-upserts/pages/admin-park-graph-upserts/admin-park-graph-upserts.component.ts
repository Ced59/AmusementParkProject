import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkGraphUpsertChange, ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { SafeExternalUrlPipe } from '@shared/pipes';

type ParkGraphUpsertChangeTypeFilter = 'All' | 'Created' | 'Updated' | 'Unchanged' | 'Warning' | 'Skipped';

interface ParkGraphUpsertMessageGroup {
  entityType: string;
  displayName: string;
  messages: string[];
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
  protected readonly changeTypeFilters: ParkGraphUpsertChangeTypeFilter[] = ['All', 'Created', 'Updated', 'Unchanged', 'Warning', 'Skipped'];
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

  constructor(
    private readonly route: ActivatedRoute,
    private readonly parksApi: ParksApiService,
    private readonly parkGraphUpsertsApi: ParkGraphUpsertsApiService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
  }

  ngOnInit(): void {
    this.selectParkFromQueryParams(this.route.snapshot.queryParamMap);
  }

  protected searchParks(): void {
    const query: string = this.searchTerm.trim();
    this.uiError = null;
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
        error: (): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.searchFailed';
        }
      });
  }

  protected selectPark(park: Park): void {
    this.selectedPark = park;
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.uiError = null;
  }

  protected clearSelectedPark(): void {
    this.selectedPark = null;
    this.previewResult = null;
    this.lastAppliedResult = null;
  }

  protected exportSelectedParkJson(): void {
    this.uiError = null;

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
        error: (): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.exportFailed';
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
      this.changeDetectorRef.markForCheck();
    };
    reader.onerror = (): void => {
      this.uiError = 'admin.parkGraphUpserts.errors.fileReadFailed';
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
    this.parkGraphUpsertsApi.preview(request)
      .pipe(finalize((): void => {
        this.isPreviewing = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (result: ParkGraphUpsertResult): void => {
          this.previewResult = result;
        },
        error: (): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.previewFailed';
        }
      });
  }

  protected apply(): void {
    const request: ParkGraphUpsertRequest | null = this.buildRequest();
    if (!request || !this.previewResult?.canApply) {
      return;
    }

    this.isApplying = true;
    this.lastAppliedResult = null;
    this.parkGraphUpsertsApi.apply(request)
      .pipe(finalize((): void => {
        this.isApplying = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (result: ParkGraphUpsertResult): void => {
          this.lastAppliedResult = result;
          this.previewResult = result;
        },
        error: (): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.applyFailed';
        }
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
