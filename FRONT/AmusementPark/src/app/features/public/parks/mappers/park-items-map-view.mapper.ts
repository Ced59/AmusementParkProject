import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { getParkItemCategoryTranslationKey, getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveParkItemMarkerIconKind } from '@shared/utils/maps/map-marker-icon-kind.resolver';
import { buildParkItemMapDetailRouteCommands } from '@shared/services/maps/map-marker-detail-route.helpers';
import { buildPublicParkItemRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import {
  ParkItemsMapFilterOptionViewModel,
  ParkItemsMapMarkerViewModel,
  ParkItemsMapUnlocatedItemViewModel,
  ParkItemsMapViewModel
} from '../models/park-items-map-view.model';

export function mapParkItemsToMapViewModel(
  park: Park,
  items: readonly ParkItem[],
  zones: readonly ParkZone[],
  currentLanguage: string
): ParkItemsMapViewModel {
  const markers: ParkItemsMapMarkerViewModel[] = items
    .filter((item: ParkItem) => item.isVisible !== false)
    .filter((item: ParkItem) => hasValidParkItemPosition(item))
    .map((item: ParkItem) => mapParkItemToMarker(item, zones, park, currentLanguage))
    .sort((left: ParkItemsMapMarkerViewModel, right: ParkItemsMapMarkerViewModel) => left.title?.localeCompare(right.title ?? '') ?? 0);

  return {
    parkId: normalizeOptionalString(park.id),
    parkName: normalizeOptionalString(park.name),
    language: currentLanguage,
    center: resolveMapCenter(park, markers),
    markers,
    unlocatedItems: mapUnlocatedItems(park, items, currentLanguage),
    categoryFilters: buildCategoryFilters(markers),
    zoneFilters: buildZoneFilters(markers, zones),
    hasItemMarkers: markers.length > 0
  };
}

function mapUnlocatedItems(
  park: Park,
  items: readonly ParkItem[],
  currentLanguage: string
): ParkItemsMapUnlocatedItemViewModel[] {
  return items
    .filter((item: ParkItem) => item.isVisible !== false)
    .filter((item: ParkItem) => !hasValidParkItemPosition(item))
    .map((item: ParkItem) => ({
      id: item.id ?? null,
      name: item.name,
      categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
      typeLabelKey: getParkItemTypeTranslationKey(item.type),
      detailLink: buildPublicParkItemRouteCommands({
        language: currentLanguage,
        parkId: park.id,
        parkName: park.name,
        itemId: item.id,
        itemName: item.name
      })
    }))
    .sort((left: ParkItemsMapUnlocatedItemViewModel, right: ParkItemsMapUnlocatedItemViewModel) => left.name.localeCompare(right.name));
}

function mapParkItemToMarker(
  item: ParkItem,
  zones: readonly ParkZone[],
  park: Park,
  currentLanguage: string
): ParkItemsMapMarkerViewModel {
  const zoneName: string | null = resolveZoneName(item.zoneId ?? null, zones);
  const details: string[] = [
    zoneName ? `${zoneName}` : ''
  ].filter((value: string) => value.trim().length > 0);
  const detailTranslationKeys: string[] = normalizeOptionalString(item.type)
    ? [getParkItemTypeTranslationKey(item.type)]
    : [];

  return {
    id: item.id ?? `${item.name}-${item.latitude}-${item.longitude}`,
    itemId: normalizeOptionalString(item.id),
    itemName: item.name,
    lat: item.latitude!,
    lng: item.longitude!,
    title: item.name,
    subtitle: item.category,
    subtitleTranslationKey: getParkItemCategoryTranslationKey(item.category),
    directionsActionEnabled: true,
    iconKind: resolveParkItemMarkerIconKind({
      category: item.category,
      type: item.type,
      subtype: item.subtype ?? null
    }),
    details,
    detailTranslationKeys,
    detailActionRouteCommands: buildParkItemMapDetailRouteCommands({
      language: currentLanguage,
      parkId: park.id,
      parkName: park.name,
      itemId: item.id,
      itemName: item.name
    }),
    category: item.category,
    zoneId: item.zoneId ?? null
  };
}

function buildCategoryFilters(markers: readonly ParkItemsMapMarkerViewModel[]): ParkItemsMapFilterOptionViewModel[] {
  const countsByCategory: Map<string, number> = new Map<string, number>();

  for (const marker of markers) {
    countsByCategory.set(marker.category, (countsByCategory.get(marker.category) ?? 0) + 1);
  }

  return Array.from(countsByCategory.entries())
    .sort((left: [string, number], right: [string, number]) => right[1] - left[1] || left[0].localeCompare(right[0]))
    .map(([category, count]: [string, number]) => ({
      key: category,
      labelKey: getParkItemCategoryTranslationKey(category),
      iconClass: resolveCategoryIcon(category),
      count
    }));
}

function buildZoneFilters(
  markers: readonly ParkItemsMapMarkerViewModel[],
  zones: readonly ParkZone[]
): ParkItemsMapFilterOptionViewModel[] {
  const countsByZoneId: Map<string, number> = new Map<string, number>();

  for (const marker of markers) {
    if (marker.zoneId) {
      countsByZoneId.set(marker.zoneId, (countsByZoneId.get(marker.zoneId) ?? 0) + 1);
    }
  }

  return zones
    .filter((zone: ParkZone) => zone.isVisible !== false)
    .filter((zone: ParkZone) => !!zone.id && countsByZoneId.has(zone.id))
    .sort((left: ParkZone, right: ParkZone) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0) || (left.name ?? '').localeCompare(right.name ?? ''))
    .map((zone: ParkZone) => ({
      key: zone.id ?? null,
      labelText: zone.name ?? '',
      iconClass: 'pi pi-map-marker',
      count: countsByZoneId.get(zone.id ?? '') ?? 0
    }));
}

function resolveMapCenter(park: Park, markers: readonly ParkItemsMapMarkerViewModel[]): [number, number] {
  if (markers.length > 0) {
    return [markers[0].lat, markers[0].lng];
  }

  if (Number.isFinite(park.latitude) && Number.isFinite(park.longitude)) {
    return [park.latitude, park.longitude];
  }

  return [0, 0];
}

function hasValidParkItemPosition(item: ParkItem): boolean {
  return item.latitude != null
    && item.longitude != null
    && Number.isFinite(item.latitude)
    && Number.isFinite(item.longitude)
    && Math.abs(item.latitude) <= 90
    && Math.abs(item.longitude) <= 180
    && !(item.latitude === 0 && item.longitude === 0);
}

function resolveZoneName(zoneId: string | null, zones: readonly ParkZone[]): string | null {
  if (!zoneId) {
    return null;
  }

  return zones.find((zone: ParkZone) => zone.id === zoneId)?.name?.trim() || null;
}

function resolveCategoryIcon(category: string): string {
  switch (category) {
    case 'Attraction':
      return 'pi pi-star';
    case 'Restaurant':
      return 'pi pi-shopping-cart';
    case 'Hotel':
      return 'pi pi-building';
    case 'Animal':
      return 'pi pi-heart';
    case 'Show':
      return 'pi pi-ticket';
    case 'Shop':
      return 'pi pi-shopping-bag';
    case 'Service':
      return 'pi pi-wrench';
    case 'Transport':
      return 'pi pi-send';
    default:
      return 'pi pi-map-marker';
  }
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}
