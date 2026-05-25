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

@Component({
  selector: 'app-admin-park-graph-upserts',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './admin-park-graph-upserts.component.html',
  styleUrl: './admin-park-graph-upserts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminParkGraphUpsertsComponent {
  protected searchTerm: string = '';
  protected searchResults: Park[] = [];
  protected selectedPark: Park | null = null;
  protected createIfMissing: boolean = false;
  protected replaceCollections: boolean = false;
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
  }

  protected clearSelectedPark(): void {
    this.selectedPark = null;
    this.previewResult = null;
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

  protected get canApply(): boolean {
    return Boolean(this.previewResult?.canApply) && !this.isApplying && !this.isPreviewing;
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
