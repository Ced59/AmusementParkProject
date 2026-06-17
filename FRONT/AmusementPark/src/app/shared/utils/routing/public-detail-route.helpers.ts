import { buildEntitySlug, buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { resolveSupportedLanguage } from './localized-route.helpers';

export interface PublicParkRouteTarget {
  language: string | null | undefined;
  parkId: string | null | undefined;
  parkName: string | null | undefined;
}

export interface PublicParkZoneRouteTarget extends PublicParkRouteTarget {
  zoneId: string | null | undefined;
  zoneName: string | null | undefined;
}

export interface PublicParkItemRouteTarget extends PublicParkRouteTarget {
  itemId: string | null | undefined;
  itemName: string | null | undefined;
}

export interface PublicParkVideoRouteTarget extends PublicParkRouteTarget {
  videoId: string | null | undefined;
  videoTitle: string | null | undefined;
}

export interface PublicParkItemVideoRouteTarget extends PublicParkItemRouteTarget {
  videoId: string | null | undefined;
  videoTitle: string | null | undefined;
}

export type PublicParkReferenceKind = 'operator' | 'founder' | 'manufacturer';

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

export function buildPublicParkZonesRouteCommands(target: PublicParkRouteTarget): string[] | null {
  const parkRouteCommands: string[] | null = buildPublicParkRouteCommands(target);

  if (!parkRouteCommands) {
    return null;
  }

  return [...parkRouteCommands, 'zones'];
}

export function buildPublicParkZoneRouteCommands(target: PublicParkZoneRouteTarget): string[] | null {
  const parkRouteCommands: string[] | null = buildPublicParkRouteCommands(target);
  const zoneId: string | null = normalizeRouteValue(target.zoneId);
  const zoneName: string | null = normalizeRouteValue(target.zoneName);

  if (!parkRouteCommands || !zoneId || !zoneName) {
    return null;
  }

  return [...parkRouteCommands, 'zone', zoneId, buildEntitySlug(zoneName)];
}

export function buildPublicParkImagesRouteCommands(target: PublicParkRouteTarget): string[] | null {
  const parkRouteCommands: string[] | null = buildPublicParkRouteCommands(target);

  if (!parkRouteCommands) {
    return null;
  }

  return [...parkRouteCommands, 'images'];
}

export function buildPublicParkVideosRouteCommands(target: PublicParkRouteTarget): string[] | null {
  const parkRouteCommands: string[] | null = buildPublicParkRouteCommands(target);

  if (!parkRouteCommands) {
    return null;
  }

  return [...parkRouteCommands, 'videos'];
}

export function buildPublicParkVideoRouteCommands(target: PublicParkVideoRouteTarget): string[] | null {
  const videosRouteCommands: string[] | null = buildPublicParkVideosRouteCommands(target);
  const videoId: string | null = normalizeRouteValue(target.videoId);
  const videoTitle: string | null = normalizeRouteValue(target.videoTitle);

  if (!videosRouteCommands || !videoId || !videoTitle) {
    return null;
  }

  return [...videosRouteCommands, videoId, buildEntitySlug(videoTitle)];
}

export function buildPublicParkMapRouteCommands(target: PublicParkRouteTarget): string[] | null {
  const parkRouteCommands: string[] | null = buildPublicParkRouteCommands(target);

  if (!parkRouteCommands) {
    return null;
  }

  return [...parkRouteCommands, 'map'];
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

export function buildPublicParkItemImagesRouteCommands(target: PublicParkItemRouteTarget): string[] | null {
  const itemRouteCommands: string[] | null = buildPublicParkItemRouteCommands(target);

  if (!itemRouteCommands) {
    return null;
  }

  return [...itemRouteCommands, 'images'];
}

export function buildPublicParkItemVideosRouteCommands(target: PublicParkItemRouteTarget): string[] | null {
  const itemRouteCommands: string[] | null = buildPublicParkItemRouteCommands(target);

  if (!itemRouteCommands) {
    return null;
  }

  return [...itemRouteCommands, 'videos'];
}

export function buildPublicParkItemVideoRouteCommands(target: PublicParkItemVideoRouteTarget): string[] | null {
  const videosRouteCommands: string[] | null = buildPublicParkItemVideosRouteCommands(target);
  const videoId: string | null = normalizeRouteValue(target.videoId);
  const videoTitle: string | null = normalizeRouteValue(target.videoTitle);

  if (!videosRouteCommands || !videoId || !videoTitle) {
    return null;
  }

  return [...videosRouteCommands, videoId, buildEntitySlug(videoTitle)];
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
    getPublicParkReferenceRouteSegment(target.kind),
    referenceId,
    buildEntitySlug(referenceName)
  ];
}

function normalizeRouteValue(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}

function getPublicParkReferenceRouteSegment(kind: PublicParkReferenceKind): string {
  if (kind === 'operator') {
    return 'park-operator';
  }

  if (kind === 'manufacturer') {
    return 'park-manufacturer';
  }

  return 'park-founder';
}
