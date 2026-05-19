import { buildEntitySlug, buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { resolveSupportedLanguage } from '@shared/utils/routing/localized-route.helpers';

export interface ParkMapDetailTarget {
  language: string | null | undefined;
  parkId: string | null | undefined;
  parkName: string | null | undefined;
}

export interface ParkItemMapDetailTarget extends ParkMapDetailTarget {
  itemId: string | null | undefined;
  itemName: string | null | undefined;
}

export function buildParkMapDetailRouteCommands(target: ParkMapDetailTarget): string[] | null {
  const parkId: string | null = normalizeRouteValue(target.parkId);
  const parkName: string | null = normalizeRouteValue(target.parkName);

  if (!parkId || !parkName) {
    return null;
  }

  return [
    '/',
    resolveSupportedLanguage(target.language),
    'park',
    parkId,
    buildParkSlug(parkName)
  ];
}

export function buildParkItemMapDetailRouteCommands(target: ParkItemMapDetailTarget): string[] | null {
  const parkId: string | null = normalizeRouteValue(target.parkId);
  const parkName: string | null = normalizeRouteValue(target.parkName);
  const itemId: string | null = normalizeRouteValue(target.itemId);
  const itemName: string | null = normalizeRouteValue(target.itemName);

  if (!parkId || !parkName || !itemId || !itemName) {
    return null;
  }

  return [
    '/',
    resolveSupportedLanguage(target.language),
    'park',
    parkId,
    buildParkSlug(parkName),
    'item',
    itemId,
    buildEntitySlug(itemName)
  ];
}

function normalizeRouteValue(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}
