import { FormGroup } from '@angular/forms';

import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import {
  ATTRACTION_TYPE_OPTIONS,
  NON_ATTRACTION_TYPE_OPTIONS_BY_CATEGORY,
  PARK_ITEM_CATEGORY_OPTIONS,
  TranslationOption
} from '@shared/utils/display/display-options';

export function getAdminParkItemCategoryOptions(): Array<TranslationOption<ParkItemCategory>> {
  return [...PARK_ITEM_CATEGORY_OPTIONS];
}

export function getAdminParkItemTypeOptionsForCategory(category: ParkItemCategory): Array<TranslationOption<ParkItemType>> {
  if (category === 'Attraction') {
    return [...ATTRACTION_TYPE_OPTIONS];
  }

  return [
    ...(
      NON_ATTRACTION_TYPE_OPTIONS_BY_CATEGORY[category as Exclude<ParkItemCategory, 'Attraction'>]
      ?? NON_ATTRACTION_TYPE_OPTIONS_BY_CATEGORY.Other
    )
  ];
}

export function applyAdminParkItemCategorySelection(
  form: FormGroup,
  category: ParkItemCategory
): Array<TranslationOption<ParkItemType>> {
  const filteredTypeOptions: Array<TranslationOption<ParkItemType>> = getAdminParkItemTypeOptionsForCategory(category);
  const currentType: ParkItemType | null = form.get('type')?.value as ParkItemType | null;
  const allowedTypes: ParkItemType[] = filteredTypeOptions.map((option: TranslationOption<ParkItemType>) => option.value);

  if (!currentType || !allowedTypes.includes(currentType)) {
    form.get('type')?.setValue(allowedTypes[0]);
  }

  return filteredTypeOptions;
}
