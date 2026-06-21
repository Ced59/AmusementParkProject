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
    languageCode: string | null | undefined
  ): AdminContextualBlockInstance | null {
    const normalizedParkId: string | null = this.normalizeValue(parkId);
    const definition: AdminContextualBlockDefinition | null = this.getDefinition(type);

    if (!normalizedParkId || !definition || definition.type === 'parkItem.description') {
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
      adminRoute: ['/', normalizedLanguageCode, 'admin', 'parks', 'edit', normalizedParkId]
    };
  }

  createParkItemBlock(
    type: AdminContextualBlockType,
    parkItemId: string | null | undefined,
    parkId: string | null | undefined,
    itemName: string | null | undefined,
    languageCode: string | null | undefined
  ): AdminContextualBlockInstance | null {
    const normalizedParkItemId: string | null = this.normalizeValue(parkItemId);
    const normalizedParkId: string | null = this.normalizeValue(parkId);
    const definition: AdminContextualBlockDefinition | null = this.getDefinition(type);

    if (!normalizedParkItemId || !normalizedParkId || !definition || definition.type !== 'parkItem.description') {
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
      adminRoute: ['/', normalizedLanguageCode, 'admin', 'parks', 'edit', normalizedParkId, 'items', normalizedParkItemId]
    };
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
}
