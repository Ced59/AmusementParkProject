import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkGraphUpsertChange, ParkGraphUpsertHistoryEntry, ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';

type ParkGraphUpsertWizardStep = 'target' | 'mode' | 'graph' | 'preview';
type ParkGraphUpsertWorkspaceTab = ParkGraphUpsertWizardStep | 'expert' | 'history';
type ParkGraphUpsertMode = 'merge' | 'replaceCollections';
type ParkGraphUpsertChangeTypeFilter = 'All' | 'Created' | 'Updated' | 'Unchanged' | 'Warning' | 'Skipped';
type ParkGraphUpsertTemplateId = 'parkOnly' | 'zonesOnly' | 'zoneItems' | 'servicesNoZone' | 'references' | 'captainCoaster';

interface ParkGraphUpsertTemplate {
  id: ParkGraphUpsertTemplateId;
  titleKey: string;
  descriptionKey: string;
}

interface ParkGraphUpsertBuilderState {
  parkName: string;
  countryCode: string;
  city: string;
  websiteUrl: string;
  zoneKey: string;
  zoneName: string;
  itemKey: string;
  itemName: string;
  itemCategory: string;
  itemType: string;
  manufacturerKey: string;
  manufacturerName: string;
  imageUrl: string;
  imageAltText: string;
  metadataSource: string;
}

interface ParkGraphUpsertMessageGroup {
  entityType: string;
  displayName: string;
  messages: string[];
}

interface ParkGraphUpsertDocumentSummary {
  name: string;
  countryCode: string;
  zoneCount: number;
  itemCount: number;
  imageCount: number;
  operatorCount: number;
  founderCount: number;
  manufacturerCount: number;
}

@Component({
  selector: 'app-admin-park-graph-upserts',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './admin-park-graph-upserts.component.html',
  styleUrl: './admin-park-graph-upserts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminParkGraphUpsertsComponent {
  protected readonly wizardSteps: ParkGraphUpsertWizardStep[] = ['target', 'mode', 'graph', 'preview'];
  protected readonly templates: ParkGraphUpsertTemplate[] = [
    {
      id: 'parkOnly',
      titleKey: 'admin.parkGraphUpserts.templates.parkOnly.title',
      descriptionKey: 'admin.parkGraphUpserts.templates.parkOnly.description'
    },
    {
      id: 'zonesOnly',
      titleKey: 'admin.parkGraphUpserts.templates.zonesOnly.title',
      descriptionKey: 'admin.parkGraphUpserts.templates.zonesOnly.description'
    },
    {
      id: 'zoneItems',
      titleKey: 'admin.parkGraphUpserts.templates.zoneItems.title',
      descriptionKey: 'admin.parkGraphUpserts.templates.zoneItems.description'
    },
    {
      id: 'servicesNoZone',
      titleKey: 'admin.parkGraphUpserts.templates.servicesNoZone.title',
      descriptionKey: 'admin.parkGraphUpserts.templates.servicesNoZone.description'
    },
    {
      id: 'references',
      titleKey: 'admin.parkGraphUpserts.templates.references.title',
      descriptionKey: 'admin.parkGraphUpserts.templates.references.description'
    },
    {
      id: 'captainCoaster',
      titleKey: 'admin.parkGraphUpserts.templates.captainCoaster.title',
      descriptionKey: 'admin.parkGraphUpserts.templates.captainCoaster.description'
    }
  ];
  protected readonly changeTypeFilters: ParkGraphUpsertChangeTypeFilter[] = ['All', 'Created', 'Updated', 'Unchanged', 'Warning', 'Skipped'];

  protected activeWorkspaceTab: ParkGraphUpsertWorkspaceTab = 'target';
  protected searchTerm: string = '';
  protected searchResults: Park[] = [];
  protected selectedPark: Park | null = null;
  protected createIfMissing: boolean = false;
  protected replaceCollections: boolean = false;
  protected graphMode: ParkGraphUpsertMode = 'merge';
  protected jsonText: string = this.buildDefaultJson();
  protected builder: ParkGraphUpsertBuilderState = this.buildDefaultBuilder();
  protected previewResult: ParkGraphUpsertResult | null = null;
  protected lastAppliedResult: ParkGraphUpsertResult | null = null;
  protected historyEntries: ParkGraphUpsertHistoryEntry[] = [];
  protected changeTypeFilter: ParkGraphUpsertChangeTypeFilter = 'All';
  protected entityTypeFilter: string = 'All';
  protected isSearching: boolean = false;
  protected isPreviewing: boolean = false;
  protected isApplying: boolean = false;
  protected isLoadingHistory: boolean = false;
  protected uiError: string | null = null;

  constructor(
    private readonly parksApi: ParksApiService,
    private readonly parkGraphUpsertsApi: ParkGraphUpsertsApiService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
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
    this.createIfMissing = false;
    this.previewResult = null;
    this.lastAppliedResult = null;
  }

  protected clearSelectedPark(): void {
    this.selectedPark = null;
    this.previewResult = null;
    this.lastAppliedResult = null;
  }

  protected selectWorkspaceTab(tab: ParkGraphUpsertWorkspaceTab): void {
    this.activeWorkspaceTab = tab;
    if (tab === 'history' && this.historyEntries.length === 0) {
      this.loadHistory();
    }
  }

  protected continueFromTarget(): void {
    this.uiError = null;

    if (!this.canContinueFromTarget) {
      this.uiError = 'admin.parkGraphUpserts.errors.noParkSelected';
      this.changeDetectorRef.markForCheck();
      return;
    }

    this.activeWorkspaceTab = 'mode';
  }

  protected continueFromMode(): void {
    this.syncModeToJson();
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.activeWorkspaceTab = 'graph';
  }

  protected selectGraphMode(mode: ParkGraphUpsertMode): void {
    this.graphMode = mode;
    this.replaceCollections = mode === 'replaceCollections';
    this.syncModeToJson();
    this.previewResult = null;
    this.lastAppliedResult = null;
  }

  protected openExpertJson(): void {
    this.activeWorkspaceTab = 'expert';
  }

  protected applyTemplate(templateId: ParkGraphUpsertTemplateId): void {
    const document: Record<string, unknown> = this.buildTemplateDocument(templateId);
    this.jsonText = JSON.stringify(document, null, 2);
    this.hydrateBuilderFromDocument(document);
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.activeWorkspaceTab = 'graph';
  }

  protected applyBuilderToJson(): void {
    const document: Record<string, unknown> = this.buildDocumentFromBuilder();
    this.jsonText = JSON.stringify(document, null, 2);
    this.previewResult = null;
    this.lastAppliedResult = null;
  }

  protected loadHistory(): void {
    this.isLoadingHistory = true;
    this.parkGraphUpsertsApi.getHistory(this.selectedPark?.id ?? null, 20)
      .pipe(finalize((): void => {
        this.isLoadingHistory = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (entries: ParkGraphUpsertHistoryEntry[]): void => {
          this.historyEntries = entries;
        },
        error: (): void => {
          this.uiError = 'admin.parkGraphUpserts.errors.historyFailed';
        }
      });
  }

  protected reuseHistory(entry: ParkGraphUpsertHistoryEntry): void {
    this.jsonText = this.formatRawJson(entry.rawJson);
    this.previewResult = null;
    this.lastAppliedResult = null;
    this.createIfMissing = !entry.targetParkId;
    if (entry.targetParkId) {
      this.selectedPark = {
        id: entry.targetParkId,
        name: entry.targetParkName ?? entry.targetParkId,
        countryCode: '',
        latitude: 0,
        longitude: 0
      };
    }

    const document: Record<string, unknown> | null = this.parseJsonRecord();
    if (document) {
      this.hydrateBuilderFromDocument(document);
    }

    this.activeWorkspaceTab = 'graph';
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
          this.activeWorkspaceTab = 'preview';
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

  protected get groupedErrors(): ParkGraphUpsertMessageGroup[] {
    return this.groupMessages(this.previewResult?.errors ?? []);
  }

  protected get groupedWarnings(): ParkGraphUpsertMessageGroup[] {
    return this.groupMessages(this.previewResult?.warnings ?? []);
  }

  protected get canApply(): boolean {
    return Boolean(this.previewResult?.canApply) && !this.isApplying && !this.isPreviewing;
  }

  protected get canContinueFromTarget(): boolean {
    return Boolean(this.selectedPark) || this.createIfMissing;
  }

  protected get documentSummary(): ParkGraphUpsertDocumentSummary | null {
    const document: Record<string, unknown> | null = this.parseJsonRecord();
    if (!document) {
      return null;
    }

    const identity: Record<string, unknown> | null = this.readRecord(document, 'identity');
    const references: Record<string, unknown> | null = this.readRecord(document, 'references');

    return {
      name: this.readString(identity, 'name'),
      countryCode: this.readString(identity, 'countryCode'),
      zoneCount: this.readArrayLength(document, 'zones'),
      itemCount: this.readArrayLength(document, 'items'),
      imageCount: this.readArrayLength(document, 'images'),
      operatorCount: this.readArrayLength(references, 'operators'),
      founderCount: this.readArrayLength(references, 'founders'),
      manufacturerCount: this.readArrayLength(references, 'manufacturers')
    };
  }

  protected get isPreviewStepReady(): boolean {
    return Boolean(this.previewResult) || this.isPreviewing;
  }

  protected isWizardStepActive(step: ParkGraphUpsertWizardStep): boolean {
    return this.activeWorkspaceTab === step;
  }

  protected isWizardStepDone(step: ParkGraphUpsertWizardStep): boolean {
    if (step === 'target') {
      return this.canContinueFromTarget;
    }

    if (step === 'mode') {
      return this.activeWorkspaceTab === 'graph' || this.activeWorkspaceTab === 'preview' || this.activeWorkspaceTab === 'expert' || Boolean(this.previewResult);
    }

    if (step === 'graph') {
      return Boolean(this.previewResult);
    }

    return Boolean(this.lastAppliedResult);
  }

  protected trackWizardStep(_: number, step: ParkGraphUpsertWizardStep): string {
    return step;
  }

  protected trackPark(_: number, park: Park): string {
    return park.id ?? park.name ?? `${park.latitude}-${park.longitude}`;
  }

  protected trackChange(index: number, change: ParkGraphUpsertChange): string {
    return `${change.entityType}-${change.entityId ?? change.entityKey ?? index}`;
  }

  protected trackTemplate(_: number, template: ParkGraphUpsertTemplate): string {
    return template.id;
  }

  protected trackHistory(_: number, entry: ParkGraphUpsertHistoryEntry): string {
    return entry.id;
  }

  protected trackMessageGroup(_: number, group: ParkGraphUpsertMessageGroup): string {
    return `${group.entityType}-${group.displayName}`;
  }

  protected trackString(_: number, value: string): string {
    return value;
  }

  private buildRequest(): ParkGraphUpsertRequest | null {
    this.uiError = null;

    if (!this.selectedPark && !this.createIfMissing) {
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
      targetParkId: this.selectedPark?.id ?? null,
      createIfMissing: this.createIfMissing,
      replaceCollections: this.replaceCollections,
      document
    };
  }

  private syncModeToJson(): void {
    const document: Record<string, unknown> | null = this.parseJsonRecord();
    if (!document) {
      return;
    }

    document['mode'] = this.graphMode === 'replaceCollections' ? 'replace' : 'merge';
    this.jsonText = JSON.stringify(document, null, 2);
  }

  private buildDefaultBuilder(): ParkGraphUpsertBuilderState {
    return {
      parkName: '',
      countryCode: '',
      city: '',
      websiteUrl: '',
      zoneKey: 'main',
      zoneName: '',
      itemKey: 'item-1',
      itemName: '',
      itemCategory: 'Attraction',
      itemType: 'Attraction',
      manufacturerKey: 'manufacturer-1',
      manufacturerName: '',
      imageUrl: '',
      imageAltText: '',
      metadataSource: 'manual-builder'
    };
  }

  private buildDocumentFromBuilder(): Record<string, unknown> {
    const zones: unknown[] = this.builder.zoneName.trim()
      ? [{
          key: this.builder.zoneKey.trim() || 'main',
          name: this.builder.zoneName.trim(),
          isVisible: false
        }]
      : [];

    const manufacturers: unknown[] = this.builder.manufacturerName.trim()
      ? [{
          key: this.builder.manufacturerKey.trim() || 'manufacturer-1',
          name: this.builder.manufacturerName.trim(),
          adminReviewStatus: 'ToReview'
        }]
      : [];

    const items: unknown[] = this.builder.itemName.trim()
      ? [{
          key: this.builder.itemKey.trim() || 'item-1',
          name: this.builder.itemName.trim(),
          category: this.builder.itemCategory,
          type: this.builder.itemType,
          zoneKey: this.builder.zoneName.trim() ? (this.builder.zoneKey.trim() || 'main') : null,
          isVisible: false,
          adminReviewStatus: 'ToReview',
          attractionDetails: this.builder.manufacturerName.trim()
            ? {
                manufacturerKey: this.builder.manufacturerKey.trim() || 'manufacturer-1'
              }
            : {}
        }]
      : [];

    const images: unknown[] = this.builder.imageUrl.trim()
      ? [{
          ownerKey: this.builder.itemName.trim() ? (this.builder.itemKey.trim() || 'item-1') : 'park',
          sourceUrl: this.builder.imageUrl.trim(),
          altText: this.builder.imageAltText.trim(),
          isVisible: false
        }]
      : [];

    return {
      documentType: 'AmusementParkParkGraphUpsert',
      schemaVersion: '2026-05-25',
      mode: this.graphMode === 'replaceCollections' ? 'replace' : 'merge',
      identity: {
        name: this.builder.parkName.trim(),
        countryCode: this.builder.countryCode.trim().toUpperCase()
      },
      references: {
        operators: [],
        founders: [],
        manufacturers
      },
      park: {
        name: this.builder.parkName.trim(),
        countryCode: this.builder.countryCode.trim().toUpperCase(),
        city: this.builder.city.trim(),
        websiteUrl: this.builder.websiteUrl.trim(),
        isVisible: false,
        adminReviewStatus: 'ToReview'
      },
      zones,
      items,
      images,
      metadata: {
        source: this.builder.metadataSource.trim() || 'manual-builder'
      }
    };
  }

  private buildTemplateDocument(templateId: ParkGraphUpsertTemplateId): Record<string, unknown> {
    const baseDocument: Record<string, unknown> = this.buildDocumentFromBuilder();

    if (templateId === 'parkOnly') {
      baseDocument['zones'] = [];
      baseDocument['items'] = [];
      baseDocument['images'] = [];
      return baseDocument;
    }

    if (templateId === 'zonesOnly') {
      baseDocument['items'] = [];
      baseDocument['images'] = [];
      return baseDocument;
    }

    if (templateId === 'servicesNoZone') {
      baseDocument['zones'] = [];
      baseDocument['items'] = [{
        key: this.builder.itemKey.trim() || 'service-1',
        name: this.builder.itemName.trim() || '',
        category: 'Service',
        type: 'Service',
        isVisible: false,
        adminReviewStatus: 'ToReview'
      }];
      return baseDocument;
    }

    if (templateId === 'references') {
      baseDocument['park'] = {};
      baseDocument['zones'] = [];
      baseDocument['items'] = [];
      baseDocument['images'] = [];
      return baseDocument;
    }

    if (templateId === 'captainCoaster') {
      baseDocument['metadata'] = {
        source: 'captain-coaster',
        importProfile: 'captain-coaster-enriched'
      };
      return baseDocument;
    }

    return baseDocument;
  }

  private hydrateBuilderFromDocument(document: Record<string, unknown>): void {
    const identity: Record<string, unknown> | null = this.readRecord(document, 'identity');
    const park: Record<string, unknown> | null = this.readRecord(document, 'park');
    const references: Record<string, unknown> | null = this.readRecord(document, 'references');
    const zones: unknown[] = this.readArray(document, 'zones');
    const items: unknown[] = this.readArray(document, 'items');
    const images: unknown[] = this.readArray(document, 'images');
    const manufacturers: unknown[] = references ? this.readArray(references, 'manufacturers') : [];
    const firstZone: Record<string, unknown> | null = this.firstRecord(zones);
    const firstItem: Record<string, unknown> | null = this.firstRecord(items);
    const firstImage: Record<string, unknown> | null = this.firstRecord(images);
    const firstManufacturer: Record<string, unknown> | null = this.firstRecord(manufacturers);

    this.builder = {
      ...this.builder,
      parkName: this.readString(park, 'name') || this.readString(identity, 'name'),
      countryCode: this.readString(park, 'countryCode') || this.readString(identity, 'countryCode'),
      city: this.readString(park, 'city'),
      websiteUrl: this.readString(park, 'websiteUrl'),
      zoneKey: this.readString(firstZone, 'key') || this.builder.zoneKey,
      zoneName: this.readString(firstZone, 'name'),
      itemKey: this.readString(firstItem, 'key') || this.builder.itemKey,
      itemName: this.readString(firstItem, 'name'),
      itemCategory: this.readString(firstItem, 'category') || this.builder.itemCategory,
      itemType: this.readString(firstItem, 'type') || this.builder.itemType,
      manufacturerKey: this.readString(firstManufacturer, 'key') || this.builder.manufacturerKey,
      manufacturerName: this.readString(firstManufacturer, 'name'),
      imageUrl: this.readString(firstImage, 'sourceUrl'),
      imageAltText: this.readString(firstImage, 'altText')
    };
  }

  private formatRawJson(rawJson: string): string {
    try {
      return JSON.stringify(JSON.parse(rawJson), null, 2);
    } catch {
      return rawJson;
    }
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

  private parseJsonRecord(): Record<string, unknown> | null {
    let document: unknown;

    try {
      document = JSON.parse(this.jsonText);
    } catch {
      return null;
    }

    if (!this.isRecord(document)) {
      return null;
    }

    return document;
  }

  private readRecord(source: Record<string, unknown> | null, key: string): Record<string, unknown> | null {
    if (!source) {
      return null;
    }

    const value: unknown = source[key];
    return this.isRecord(value) ? value : null;
  }

  private readString(source: Record<string, unknown> | null, key: string): string {
    if (!source) {
      return '';
    }

    const value: unknown = source[key];
    return typeof value === 'string' ? value : '';
  }

  private readArrayLength(source: Record<string, unknown> | null, key: string): number {
    if (!source) {
      return 0;
    }

    const value: unknown = source[key];
    return Array.isArray(value) ? value.length : 0;
  }

  private readArray(source: Record<string, unknown>, key: string): unknown[] {
    const value: unknown = source[key];
    return Array.isArray(value) ? value : [];
  }

  private firstRecord(values: unknown[]): Record<string, unknown> | null {
    const first: unknown = values[0];
    return this.isRecord(first) ? first : null;
  }

  private isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
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
