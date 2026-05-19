import { buildEntitySlug, buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { resolveSupportedLanguage } from './localized-route.helpers';

export interface PublicParkRouteTarget {
  language: string | null | undefined;
  parkId: string | null | undefined;
  parkName: string | null | undefined;
}

export interface PublicParkItemRouteTarget extends PublicParkRouteTarget {
  itemId: string | null | undefined;
  itemName: string | null | undefined;
}

export type PublicParkReferenceKind = 'operator' | 'founder';

export interface PublicParkReferenceRouteTarget {
  language: string | null | undefined;
  referenceId: string | null | undefined;
  referenceName: string | null | undefined;
  kind: PublicParkReferenceKind;
}

export function buildPublicParkRouteCommands(target: PublicParkRouteTarget): string[] | null {
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

export function buildPublicParkItemsRouteCommands(target: PublicParkRouteTarget): string[] | null {
  const parkRouteCommands: string[] | null = buildPublicParkRouteCommands(target);

  if (!parkRouteCommands) {
    return null;
  }

  return [...parkRouteCommands, 'items'];
}

export function buildPublicParkItemRouteCommands(target: PublicParkItemRouteTarget): string[] | null {
  const parkRouteCommands: string[] | null = buildPublicParkRouteCommands(target);
  const itemId: string | null = normalizeRouteValue(target.itemId);
  const itemName: string | null = normalizeRouteValue(target.itemName);

  if (!parkRouteCommands || !itemId || !itemName) {
    return null;
  }

  return [
    ...parkRouteCommands,
    'item',
    itemId,
    buildEntitySlug(itemName)
  ];
}

export function buildPublicParkReferenceRouteCommands(target: PublicParkReferenceRouteTarget): string[] | null {
  const referenceId: string | null = normalizeRouteValue(target.referenceId);
  const referenceName: string | null = normalizeRouteValue(target.referenceName);

  if (!referenceId || !referenceName) {
    return null;
  }

  return [
    '/',
    resolveSupportedLanguage(target.language),
    target.kind === 'operator' ? 'park-operator' : 'park-founder',
    referenceId,
    buildEntitySlug(referenceName)
  ];
}

function normalizeRouteValue(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}
