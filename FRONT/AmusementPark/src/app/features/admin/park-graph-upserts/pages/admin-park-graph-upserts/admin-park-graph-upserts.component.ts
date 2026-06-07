import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkGraphUpsertChange, ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';

type ParkGraphUpsertWizardStep = 'target' | 'mode' | 'graph' | 'preview';
type ParkGraphUpsertWorkspaceTab = ParkGraphUpsertWizardStep | 'expert';
type ParkGraphUpsertMode = 'merge' | 'replaceCollections';

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

  protected activeWorkspaceTab: ParkGraphUpsertWorkspaceTab = 'target';
  protected searchTerm: string = '';
  protected searchResults: Park[] = [];
  protected selectedPark: Park | null = null;
  protected createIfMissing: boolean = false;
  protected replaceCollections: boolean = false;
  protected graphMode: ParkGraphUpsertMode = 'merge';
  protected jsonText: string = this.buildDefaultJson();
  protected previewResult: ParkGraphUpsertResult | null = null;
  protected lastAppliedResult: ParkGraphUpsertResult | null = null;
  protected isSearching: boolean = false;
  protected isPreviewing: boolean = false;
  protected isApplying: boolean = false;
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
