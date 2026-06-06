import {
  ParkItemDetailRowViewModel,
  ParkItemDetailSpecGroupViewModel
} from '../models/park-item-detail-view.model';

export function pushGroup(
  groups: ParkItemDetailSpecGroupViewModel[],
  titleKey: string,
  iconClass: string,
  rows: ParkItemDetailRowViewModel[]
): void {
  if (rows.length === 0) {
    return;
  }

  groups.push({ titleKey, iconClass, rows });
}

export function pushRow(
  rows: ParkItemDetailRowViewModel[],
  labelKey: string,
  value: string | null | undefined,
  valueKey: string | null = null,
  iconClass: string | null = null,
  routerLink: string[] | null = null,
  queryParams: Record<string, string> | null = null
): void {
  const trimmedValue: string = value?.trim() ?? '';

  if (trimmedValue.length === 0 && !valueKey) {
    return;
  }

  rows.push({
    labelKey,
    value: trimmedValue,
    valueKey,
    iconClass,
    isTextualValue: isTextualDetailValue(labelKey, trimmedValue, valueKey),
    routerLink,
    queryParams
  });
}

export function isTextualDetailValue(labelKey: string, value: string, valueKey: string | null): boolean {
  if (valueKey !== null) {
    return true;
  }

  if (labelKey === 'parkItems.fields.status') {
    return true;
  }

  return value.length > 8 && /[A-Za-zÀ-ÿ]/.test(value);
}
