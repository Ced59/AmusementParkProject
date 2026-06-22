import { Injectable } from '@angular/core';

import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import {
  AdminContextualBlockDefinition,
  AdminContextualBlockInstance,
  AdminContextualBlockType
} from '../models/admin-contextual-block.model';

const SUPPORTED_LANGUAGE_CODES: readonly string[] = LANGUAGES.map((language: LanguageOption): string => language.value);

const CONTEXTUAL_BLOCK_DEFINITIONS: readonly AdminContextualBlockDefinition[] = [
  {
    type: 'park.hero',
    labelKey: 'admin.contextualBlocks.blocks.parkHero.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkHero.description',
    iconClass: 'pi pi-image',
    capabilities: ['fullAdminEdit', 'targetedChildAdd', 'boundedJsonExportPlanned', 'boundedJsonUpsertPlanned', 'formEditPlanned'],
    jsonScope: [
      'park.id',
      'park.name',
      'park.type',
      'park.logoImageId',
      'park.heroImageId',
      'park.isVisible'
    ],
    localizedLanguageCodes: []
  },
  {
    type: 'park.description',
    labelKey: 'admin.contextualBlocks.blocks.parkDescription.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkDescription.description',
    iconClass: 'pi pi-align-left',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'contextualFormEdit'],
    jsonScope: [
      'park.id',
      'park.descriptions[*].languageCode',
      'park.descriptions[*].value'
    ],
    localizedLanguageCodes: SUPPORTED_LANGUAGE_CODES
  },
  {
    type: 'park.images',
    labelKey: 'admin.contextualBlocks.blocks.parkImages.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkImages.description',
    iconClass: 'pi pi-images',
    capabilities: ['fullAdminEdit', 'contextualPhotoAdd'],
    jsonScope: [
      'park.id',
      'image.file',
      'image.sourceUrl',
      'image.category',
      'image.tagIds[*]',
      'image.description',
      'image.isPublished',
      'image.setAsCurrent',
      'image.withWatermark'
    ],
    localizedLanguageCodes: []
  },
  {
    type: 'park.practical',
    labelKey: 'admin.contextualBlocks.blocks.parkPractical.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkPractical.description',
    iconClass: 'pi pi-info-circle',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'formEditPlanned'],
    jsonScope: [
      'park.id',
      'park.countryCode',
      'park.city',
      'park.street',
      'park.postalCode',
      'park.websiteUrl',
      'park.founderId',
      'park.operatorId',
      'park.latitude',
      'park.longitude'
    ],
    localizedLanguageCodes: []
  },
  {
    type: 'park.location',
    labelKey: 'admin.contextualBlocks.blocks.parkLocation.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkLocation.description',
    iconClass: 'pi pi-map-marker',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'contextualFormEdit'],
    jsonScope: [
      'park.id',
      'park.latitude',
      'park.longitude'
    ],
    localizedLanguageCodes: []
  },
  {
    type: 'parkItem.description',
    labelKey: 'admin.contextualBlocks.blocks.parkItemDescription.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkItemDescription.description',
    iconClass: 'pi pi-align-left',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'contextualFormEdit'],
    jsonScope: [
      'parkItem.id',
      'parkItem.parkId',
      'parkItem.descriptions[*].languageCode',
      'parkItem.descriptions[*].value'
    ],
    localizedLanguageCodes: SUPPORTED_LANGUAGE_CODES
  },
  {
    type: 'parkItem.images',
    labelKey: 'admin.contextualBlocks.blocks.parkItemImages.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkItemImages.description',
    iconClass: 'pi pi-images',
    capabilities: ['fullAdminEdit', 'contextualPhotoAdd'],
    jsonScope: [
      'parkItem.id',
      'parkItem.parkId',
      'image.file',
      'image.sourceUrl',
      'image.category',
      'image.tagIds[*]',
      'image.description',
      'image.isPublished',
      'image.setAsCurrent',
      'image.withWatermark'
    ],
    localizedLanguageCodes: []
  },
  {
    type: 'parkItem.location',
    labelKey: 'admin.contextualBlocks.blocks.parkItemLocation.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkItemLocation.description',
    iconClass: 'pi pi-map-marker',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'contextualFormEdit'],
    jsonScope: [
      'parkItem.id',
      'parkItem.parkId',
      'parkItem.latitude',
      'parkItem.longitude'
    ],
    localizedLanguageCodes: []
  },
  {
    type: 'reference.manufacturer',
    labelKey: 'admin.contextualBlocks.blocks.manufacturerReference.label',
    descriptionKey: 'admin.contextualBlocks.blocks.manufacturerReference.description',
    iconClass: 'pi pi-wrench',
    capabilities: ['fullAdminEdit', 'parkGraphUpsertDraft'],
    jsonScope: [
      'references.manufacturers[*].id',
      'references.manufacturers[*].name',
      'references.manufacturers[*].legalName',
      'references.manufacturers[*].foundedYear',
      'references.manufacturers[*].closedYear',
      'references.manufacturers[*].contactDetails',
      'references.manufacturers[*].isVisible',
      'references.manufacturers[*].biography[*].languageCode',
      'references.manufacturers[*].biography[*].value',
      'references.manufacturers[*].adminReviewStatus'
    ],
    localizedLanguageCodes: SUPPORTED_LANGUAGE_CODES
  }
];

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockRegistryService {
  private readonly definitionsByType = new Map<AdminContextualBlockType, AdminContextualBlockDefinition>(
    CONTEXTUAL_BLOCK_DEFINITIONS.map((definition: AdminContextualBlockDefinition) => [definition.type, definition])
  );

  getDefinitions(): readonly AdminContextualBlockDefinition[] {
    return CONTEXTUAL_BLOCK_DEFINITIONS;
  }

  getDefinition(type: AdminContextualBlockType): AdminContextualBlockDefinition | null {
    return this.definitionsByType.get(type) ?? null;
  }

  createParkBlock(
    type: AdminContextualBlockType,
    parkId: string | null | undefined,
    parkName: string | null | undefined,
    languageCode: string | null | undefined,
    locationFallbackCenter: readonly [number, number] | null = null
  ): AdminContextualBlockInstance | null {
    const normalizedParkId: string | null = this.normalizeValue(parkId);
    const definition: AdminContextualBlockDefinition | null = this.getDefinition(type);

    if (!normalizedParkId || !definition || !this.isParkBlockType(definition.type)) {
      return null;
    }

    const normalizedLanguageCode: string = this.normalizeLanguageCode(languageCode);

    return {
      ...definition,
      id: `${definition.type}:${normalizedParkId}`,
      entityType: 'Park',
      entityId: normalizedParkId,
      contextLabel: this.normalizeValue(parkName) ?? normalizedParkId,
      ids: {
        parkId: normalizedParkId
      },
      locationFallbackCenter: this.normalizeFallbackCenter(locationFallbackCenter),
      adminRoute: ['/', normalizedLanguageCode, 'admin', 'parks', 'edit', normalizedParkId]
    };
  }

  createParkItemBlock(
    type: AdminContextualBlockType,
    parkItemId: string | null | undefined,
    parkId: string | null | undefined,
    itemName: string | null | undefined,
    languageCode: string | null | undefined,
    locationFallbackCenter: readonly [number, number] | null = null
  ): AdminContextualBlockInstance | null {
    const normalizedParkItemId: string | null = this.normalizeValue(parkItemId);
    const normalizedParkId: string | null = this.normalizeValue(parkId);
    const definition: AdminContextualBlockDefinition | null = this.getDefinition(type);

    if (!normalizedParkItemId || !normalizedParkId || !definition || !this.isParkItemBlockType(definition.type)) {
      return null;
    }

    const normalizedLanguageCode: string = this.normalizeLanguageCode(languageCode);

    return {
      ...definition,
      id: `${definition.type}:${normalizedParkItemId}`,
      entityType: 'ParkItem',
      entityId: normalizedParkItemId,
      contextLabel: this.normalizeValue(itemName) ?? normalizedParkItemId,
      ids: {
        parkId: normalizedParkId,
        parkItemId: normalizedParkItemId
      },
      locationFallbackCenter: this.normalizeFallbackCenter(locationFallbackCenter),
      adminRoute: ['/', normalizedLanguageCode, 'admin', 'parks', 'edit', normalizedParkId, 'items', normalizedParkItemId]
    };
  }

  createManufacturerBlock(
    manufacturerId: string | null | undefined,
    manufacturerName: string | null | undefined,
    languageCode: string | null | undefined,
    parkGraphUpsertDraftJson: string | null | undefined,
    parkGraphUpsertFileName: string | null | undefined
  ): AdminContextualBlockInstance | null {
    const normalizedManufacturerId: string | null = this.normalizeValue(manufacturerId);
    const definition: AdminContextualBlockDefinition | null = this.getDefinition('reference.manufacturer');

    if (!normalizedManufacturerId || !definition) {
      return null;
    }

    const normalizedLanguageCode: string = this.normalizeLanguageCode(languageCode);

    return {
      ...definition,
      id: `${definition.type}:${normalizedManufacturerId}`,
      entityType: 'AttractionManufacturer',
      entityId: normalizedManufacturerId,
      contextLabel: this.normalizeValue(manufacturerName) ?? normalizedManufacturerId,
      ids: {
        manufacturerId: normalizedManufacturerId
      },
      locationFallbackCenter: null,
      adminRoute: ['/', normalizedLanguageCode, 'admin', 'manufacturers', 'edit', normalizedManufacturerId],
      parkGraphUpsertDraftJson: this.normalizeValue(parkGraphUpsertDraftJson),
      parkGraphUpsertFileName: this.normalizeFileName(parkGraphUpsertFileName, normalizedManufacturerId)
    };
  }

  private isParkItemBlockType(type: AdminContextualBlockType): boolean {
    return type === 'parkItem.description' || type === 'parkItem.images' || type === 'parkItem.location';
  }

  private isParkBlockType(type: AdminContextualBlockType): boolean {
    return type === 'park.hero'
      || type === 'park.description'
      || type === 'park.images'
      || type === 'park.location'
      || type === 'park.practical';
  }

  private normalizeLanguageCode(languageCode: string | null | undefined): string {
    const normalizedLanguageCode: string | null = this.normalizeValue(languageCode);

    return normalizedLanguageCode && SUPPORTED_LANGUAGE_CODES.includes(normalizedLanguageCode)
      ? normalizedLanguageCode
      : 'en';
  }

  private normalizeValue(value: string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    const trimmedValue: string = value.trim();

    return trimmedValue.length > 0 ? trimmedValue : null;
  }

  private normalizeFallbackCenter(value: readonly [number, number] | null): readonly [number, number] | null {
    if (!value || value.length !== 2) {
      return null;
    }

    const latitude: number = value[0];
    const longitude: number = value[1];

    return Number.isFinite(latitude)
      && Number.isFinite(longitude)
      && latitude >= -90
      && latitude <= 90
      && longitude >= -180
      && longitude <= 180
      ? [latitude, longitude]
      : null;
  }

  private normalizeFileName(value: string | null | undefined, fallbackId: string): string {
    const normalizedValue: string | null = this.normalizeValue(value);

    return normalizedValue ?? `manufacturer-${fallbackId}-manufacturer-upsert.json`;
  }
}
