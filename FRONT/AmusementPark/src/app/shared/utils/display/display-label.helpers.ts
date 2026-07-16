import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkAudienceClassification, ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkType } from '@app/models/parks/park-type';

export function getParkTypeTranslationKey(type: ParkType | string | null | undefined): string {
  return `admin.parks.types.${normalizeTranslationSegment(type, 'notSpecified')}`;
}

export function getParkAudienceClassificationTranslationKey(classification: ParkAudienceClassification | ParkAudienceClassificationFilter | string | null | undefined): string {
  const normalizedSegment: string = normalizeTranslationSegment(classification, 'notSpecified');
  return `admin.parks.audienceClassifications.${normalizedSegment === 'unspecified' ? 'notSpecified' : normalizedSegment}`;
}

export function getParkItemCategoryTranslationKey(category: ParkItemCategory | string | null | undefined): string {
  return `parkExplorer.categories.${normalizeTranslationSegment(category, 'other')}`;
}

export function getParkItemTypeTranslationKey(type: ParkItemType | string | null | undefined): string {
  return `parkExplorer.types.${normalizeTranslationSegment(type, 'other')}`;
}

export function getSearchCategoryTranslationKey(category: string | null | undefined): string {
  const normalizedCategory: string = normalizeTranslationSegment(category, 'park')
    .replace(/\s+/g, '')
    .toLowerCase();

  switch (normalizedCategory) {
    case 'parks':
      return 'home.categories.park';
    case 'parkitems':
      return 'home.categories.parkItems';
    case 'attractionswithstandalone':
    case 'attractionwithstandalone':
      return 'home.categories.attractionsWithStandalone';
    case 'standaloneattractions':
    case 'standaloneattraction':
      return 'home.categories.standaloneAttractions';
    case 'attractions':
      return 'home.categories.attraction';
    case 'restaurants':
      return 'home.categories.restaurant';
    case 'hotels':
      return 'home.categories.hotel';
    case 'animals':
      return 'home.categories.animal';
    case 'shows':
      return 'home.categories.show';
    case 'shops':
      return 'home.categories.shop';
    case 'services':
      return 'home.categories.service';
    case 'transports':
      return 'home.categories.transport';
    case 'operators':
      return 'home.categories.operators';
    case 'manufacturers':
      return 'home.categories.manufacturers';
    case 'founders':
      return 'home.categories.founders';
    default:
      return `home.categories.${normalizedCategory || 'park'}`;
  }
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
