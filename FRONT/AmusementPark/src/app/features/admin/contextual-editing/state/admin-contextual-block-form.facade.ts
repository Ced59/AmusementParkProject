import { Inject, Injectable, Signal, signal } from '@angular/core';
import { finalize } from 'rxjs';

import {
  ContextualBlockExportDocument,
  ContextualBlockLocalizedText,
  ContextualLocationBlock,
  ContextualLocalizedDescriptionBlock
} from '@shared/models/admin/contextual-block-export.models';
import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { MapMarker } from '@app/models/map/map-marker';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  ADMIN_CONTEXTUAL_BLOCK_FORM_DATA_PORT,
  AdminContextualBlockFormDataPort
} from './admin-contextual-block-form-data.ports';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';

export interface AdminContextualBlockLocalizedFormField {
  readonly languageCode: string;
  readonly value: string;
}

export interface AdminContextualBlockLocationForm {
  readonly latitude: number | null;
  readonly longitude: number | null;
  readonly mapCenter: readonly [number, number];
  readonly mapZoom: number;
  readonly mapMarkers: readonly MapMarker[];
}

const DEFAULT_LOCATION_CENTER: readonly [number, number] = [48.8566, 2.3522];

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockFormFacade {
  private readonly localizedFieldsSignal = signal<readonly AdminContextualBlockLocalizedFormField[]>([]);
  private readonly locationFormSignal = signal<AdminContextualBlockLocationForm | null>(null);
  private readonly isLoadingSignal = signal<boolean>(false);
  private readonly isSavingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly successKeySignal = signal<string | null>(null);
  private activeBlockId: string | null = null;
  private currentDescriptionDocument: ContextualBlockExportDocument<ContextualLocalizedDescriptionBlock> | null = null;
  private currentLocationDocument: ContextualBlockExportDocument<ContextualLocationBlock> | null = null;

  public readonly localizedFields: Signal<readonly AdminContextualBlockLocalizedFormField[]> = this.localizedFieldsSignal.asReadonly();
  public readonly locationForm: Signal<AdminContextualBlockLocationForm | null> = this.locationFormSignal.asReadonly();
  public readonly isLoading: Signal<boolean> = this.isLoadingSignal.asReadonly();
  public readonly isSaving: Signal<boolean> = this.isSavingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly successKey: Signal<string | null> = this.successKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_CONTEXTUAL_BLOCK_FORM_DATA_PORT) private readonly contextualBlocksApi: AdminContextualBlockFormDataPort,
    private readonly refreshEvents: AdminContextualBlockRefreshEvents
  ) {
  }

  canEditForm(block: AdminContextualBlockInstance | null): boolean {
    return Boolean(block?.capabilities.includes('contextualFormEdit'));
  }

  resetForBlock(block: AdminContextualBlockInstance | null): void {
    const nextBlockId: string | null = block?.id ?? null;
    if (this.activeBlockId === nextBlockId) {
      return;
    }

    this.activeBlockId = nextBlockId;
    this.currentDescriptionDocument = null;
    this.currentLocationDocument = null;
    this.localizedFieldsSignal.set([]);
    this.locationFormSignal.set(null);
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.isLoadingSignal.set(false);
    this.isSavingSignal.set(false);

    if (block !== null && this.canEditForm(block)) {
      this.loadForm(block);
    }
  }

  loadForm(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);

    if (!this.canEditForm(block)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.formUnavailable');
      return;
    }

    if (!block.entityId.trim()) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.formLoadError');
      return;
    }

    this.isLoadingSignal.set(true);
    if (this.isLocationBlock(block)) {
      this.loadLocationForm(block);
      return;
    }

    this.loadLocalizedDescriptionForm(block);
  }

  updateLocalizedValue(languageCode: string, value: string): void {
    this.localizedFieldsSignal.update((fields: readonly AdminContextualBlockLocalizedFormField[]) => fields.map((field: AdminContextualBlockLocalizedFormField) => {
      if (field.languageCode !== languageCode) {
        return field;
      }

      return {
        ...field,
        value,
      };
    }));
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
  }

  updateLocationPosition(latitude: number, longitude: number, block: AdminContextualBlockInstance): void {
    this.setLocationForm(latitude, longitude, block);
  }

  updateLocationLatitude(value: string, block: AdminContextualBlockInstance): void {
    const currentForm: AdminContextualBlockLocationForm | null = this.locationFormSignal();
    this.setLocationForm(this.parseNullableNumber(value), currentForm?.longitude ?? null, block);
  }

  updateLocationLongitude(value: string, block: AdminContextualBlockInstance): void {
    const currentForm: AdminContextualBlockLocationForm | null = this.locationFormSignal();
    this.setLocationForm(currentForm?.latitude ?? null, this.parseNullableNumber(value), block);
  }

  clearLocation(block: AdminContextualBlockInstance): void {
    this.setLocationForm(null, null, block);
  }

  saveForm(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);

    if (this.isLocationBlock(block)) {
      this.saveLocationForm(block);
      return;
    }

    this.saveLocalizedDescriptionForm(block);
  }

  private loadLocalizedDescriptionForm(block: AdminContextualBlockInstance): void {
    this.isLoadingSignal.set(true);
    this.contextualBlocksApi.getBlockExportDocument<ContextualLocalizedDescriptionBlock>(block.type, block.entityId)
      .pipe(finalize((): void => this.isLoadingSignal.set(false)))
      .subscribe({
        next: (document: ContextualBlockExportDocument<ContextualLocalizedDescriptionBlock>): void => {
          if (!this.isLocalizedDescriptionDocument(document, block)) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formLoadError');
            return;
          }

          this.currentDescriptionDocument = document;
          this.localizedFieldsSignal.set(document.block.descriptions.map((description: ContextualBlockLocalizedText) => ({
            languageCode: description.languageCode,
            value: description.value ?? ''
          })));
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.formLoadError');
        }
      });
  }

  private loadLocationForm(block: AdminContextualBlockInstance): void {
    this.isLoadingSignal.set(true);
    this.contextualBlocksApi.getBlockExportDocument<ContextualLocationBlock>(block.type, block.entityId)
      .pipe(finalize((): void => this.isLoadingSignal.set(false)))
      .subscribe({
        next: (document: ContextualBlockExportDocument<ContextualLocationBlock>): void => {
          if (!this.isLocationDocument(document, block)) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formLoadError');
            return;
          }

          this.currentLocationDocument = document;
          this.setLocationForm(document.block.latitude, document.block.longitude, block);
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.formLoadError');
        }
      });
  }

  private saveLocalizedDescriptionForm(block: AdminContextualBlockInstance): void {
    const currentDocument: ContextualBlockExportDocument<ContextualLocalizedDescriptionBlock> | null = this.currentDescriptionDocument;

    if (!this.canEditForm(block) || !currentDocument?.block) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
      return;
    }

    const document: ContextualBlockExportDocument<ContextualLocalizedDescriptionBlock> = {
      ...currentDocument,
      block: {
        ...currentDocument.block,
        descriptions: this.localizedFieldsSignal().map((field: AdminContextualBlockLocalizedFormField) => ({
          languageCode: field.languageCode,
          value: field.value.length === 0 ? null : field.value
        }))
      }
    };

    this.isSavingSignal.set(true);
    this.contextualBlocksApi.applyBlock(block.type, block.entityId, document)
      .pipe(finalize((): void => this.isSavingSignal.set(false)))
      .subscribe({
        next: (result: ContextualBlockPreviewResult): void => {
          if (!result.canApply) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
            return;
          }

          if (!result.isApplied) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formNoChanges');
            return;
          }

          this.currentDescriptionDocument = document;
          this.successKeySignal.set('admin.contextualBlocks.drawer.formSaveSucceeded');
          this.refreshEvents.notifyBlockApplied({
            blockType: block.type,
            entityType: block.entityType,
            entityId: result.target.entityId,
            appliedAtUtc: new Date().toISOString()
          });
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
        }
      });
  }

  private saveLocationForm(block: AdminContextualBlockInstance): void {
    const currentDocument: ContextualBlockExportDocument<ContextualLocationBlock> | null = this.currentLocationDocument;
    const locationForm: AdminContextualBlockLocationForm | null = this.locationFormSignal();

    if (!this.canEditForm(block) || !currentDocument?.block || !locationForm) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
      return;
    }

    if (!this.isValidLocationPair(locationForm.latitude, locationForm.longitude) && (locationForm.latitude !== null || locationForm.longitude !== null)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.locationInvalid');
      return;
    }

    const document: ContextualBlockExportDocument<ContextualLocationBlock> = {
      ...currentDocument,
      block: {
        ...currentDocument.block,
        latitude: locationForm.latitude,
        longitude: locationForm.longitude
      }
    };

    this.isSavingSignal.set(true);
    this.contextualBlocksApi.applyBlock(block.type, block.entityId, document)
      .pipe(finalize((): void => this.isSavingSignal.set(false)))
      .subscribe({
        next: (result: ContextualBlockPreviewResult): void => {
          if (!result.canApply) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
            return;
          }

          if (!result.isApplied) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formNoChanges');
            return;
          }

          this.currentLocationDocument = document;
          this.successKeySignal.set('admin.contextualBlocks.drawer.formSaveSucceeded');
          this.refreshEvents.notifyBlockApplied({
            blockType: block.type,
            entityType: block.entityType,
            entityId: result.target.entityId,
            appliedAtUtc: new Date().toISOString()
          });
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
        }
      });
  }

  private isLocalizedDescriptionDocument(
    document: ContextualBlockExportDocument<ContextualLocalizedDescriptionBlock>,
    block: AdminContextualBlockInstance
  ): document is ContextualBlockExportDocument<ContextualLocalizedDescriptionBlock> & { block: ContextualLocalizedDescriptionBlock } {
    return document.blockType === block.type &&
      document.target.entityType === block.entityType &&
      document.target.entityId === block.entityId &&
      document.block !== null &&
      this.resolveDocumentEntityId(document.block, block.entityType) === block.entityId &&
      Array.isArray(document.block.descriptions);
  }

  private resolveDocumentEntityId(block: ContextualLocalizedDescriptionBlock, entityType: string): string | null {
    if (entityType === 'Park') {
      return block.parkId;
    }

    if (entityType === 'ParkItem' && 'parkItemId' in block) {
      return block.parkItemId;
    }

    return null;
  }

  private isLocationDocument(
    document: ContextualBlockExportDocument<ContextualLocationBlock>,
    block: AdminContextualBlockInstance
  ): document is ContextualBlockExportDocument<ContextualLocationBlock> & { block: ContextualLocationBlock } {
    return document.blockType === block.type &&
      document.target.entityType === block.entityType &&
      document.target.entityId === block.entityId &&
      document.block !== null &&
      this.resolveLocationDocumentEntityId(document.block, block.entityType) === block.entityId;
  }

  private resolveLocationDocumentEntityId(block: ContextualLocationBlock, entityType: string): string | null {
    if (entityType === 'Park') {
      return block.parkId;
    }

    if (entityType === 'ParkItem' && 'parkItemId' in block) {
      return block.parkItemId;
    }

    return null;
  }

  private isLocationBlock(block: AdminContextualBlockInstance): boolean {
    return block.type === 'park.location' || block.type === 'parkItem.location';
  }

  private setLocationForm(latitudeValue: number | null, longitudeValue: number | null, block: AdminContextualBlockInstance): void {
    const latitude: number | null = this.normalizeNullableNumber(latitudeValue);
    const longitude: number | null = this.normalizeNullableNumber(longitudeValue);
    const hasValidPair: boolean = this.isValidLocationPair(latitude, longitude);
    const fallbackCenter: readonly [number, number] = this.resolveLocationFallbackCenter(block);
    const mapCenter: readonly [number, number] = hasValidPair ? [latitude as number, longitude as number] : fallbackCenter;
    const iconKind: MapMarker['iconKind'] = block.entityType === 'Park' ? 'park' : 'other';

    this.locationFormSignal.set({
      latitude,
      longitude,
      mapCenter,
      mapZoom: hasValidPair || block.locationFallbackCenter ? 16 : 5,
      mapMarkers: hasValidPair
        ? [{
            id: 'contextual-location',
            lat: latitude as number,
            lng: longitude as number,
            title: block.contextLabel,
            iconKind
          }]
        : []
    });
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
  }

  private resolveLocationFallbackCenter(block: AdminContextualBlockInstance): readonly [number, number] {
    return block.locationFallbackCenter ?? DEFAULT_LOCATION_CENTER;
  }

  private parseNullableNumber(value: string): number | null {
    if (value.trim().length === 0) {
      return null;
    }

    return this.normalizeNullableNumber(Number(value));
  }

  private normalizeNullableNumber(value: number | null): number | null {
    return value !== null && Number.isFinite(value) ? value : null;
  }

  private isValidLocationPair(latitude: number | null, longitude: number | null): boolean {
    return latitude !== null &&
      longitude !== null &&
      latitude >= -90 &&
      latitude <= 90 &&
      longitude >= -180 &&
      longitude <= 180;
  }
}
