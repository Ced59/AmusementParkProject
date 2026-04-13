import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkType } from '@app/models/parks/park-type';

export function getParkTypeTranslationKey(type: ParkType | string | null | undefined): string {
  return `admin.parks.types.${normalizeTranslationSegment(type, 'notSpecified')}`;
}

export function getParkItemCategoryTranslationKey(category: ParkItemCategory | string | null | undefined): string {
  return `parkExplorer.categories.${normalizeTranslationSegment(category, 'other')}`;
}

export function getParkItemTypeTranslationKey(type: ParkItemType | string | null | undefined): string {
  return `parkExplorer.types.${normalizeTranslationSegment(type, 'other')}`;
}

export function getSearchCategoryTranslationKey(category: string | null | undefined): string {
  return `home.categories.${normalizeTranslationSegment(category, 'park')}`;
}

export function getLocalizedBooleanDisplay(value: boolean | null | undefined, currentLang: string): string | null {
  if (value == null) {
    return null;
  }

  if (currentLang === 'fr') {
    return value ? 'Oui' : 'Non';
  }

  return value ? 'Yes' : 'No';
}

export function normalizeTranslationSegment(value: string | null | undefined, fallbackSegment: string): string {
  const trimmedValue: string = value?.trim() ?? '';

  if (trimmedValue.length === 0) {
    return fallbackSegment;
  }

  return `${trimmedValue.charAt(0).toLowerCase()}${trimmedValue.slice(1)}`;
}
