import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { resolveLocalizedValue, stripHtml } from '@shared/utils/localization';
import {
  getParkItemCategoryTranslationKey as getSharedParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey as getSharedParkItemTypeTranslationKey
} from './display-label.helpers';
import { buildEntitySlug } from './park-presentation.helpers';

export { buildEntitySlug };

export function getParkItemCategoryTranslationKey(category: ParkItemCategory | string | null | undefined): string {
  return getSharedParkItemCategoryTranslationKey(category);
}

export function getParkItemTypeTranslationKey(type: ParkItemType | string | null | undefined): string {
  return getSharedParkItemTypeTranslationKey(type);
}

export function resolveParkItemDescription(item: ParkItem | null | undefined, currentLang: string): string | null {
  const localizedDescription: string | undefined = resolveLocalizedValue(item?.descriptions, currentLang);
  const plainText: string = stripHtml(localizedDescription);
  return plainText.length > 0 ? plainText : null;
}
