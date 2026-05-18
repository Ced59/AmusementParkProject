import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { getParkItemCategoryTranslationKey, getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import {
  ParkItemsMapFilterOptionViewModel,
  ParkItemsMapMarkerViewModel,
  ParkItemsMapViewModel
} from '../models/park-items-map-view.model';

export function mapParkItemsToMapViewModel(
  park: Park,
  items: readonly ParkItem[],
  zones: readonly ParkZone[]
): ParkItemsMapViewModel {
  const markers: ParkItemsMapMarkerViewModel[] = items
    .filter((item: ParkItem) => item.isVisible !== false)
    .filter((item: ParkItem) => Number.isFinite(item.latitude) && Number.isFinite(item.longitude))
    .map((item: ParkItem) => mapParkItemToMarker(item, zones))
    .sort((left: ParkItemsMapMarkerViewModel, right: ParkItemsMapMarkerViewModel) => left.title?.localeCompare(right.title ?? '') ?? 0);

  return {
    center: resolveMapCenter(park, markers),
    markers,
    categoryFilters: buildCategoryFilters(markers),
    zoneFilters: buildZoneFilters(markers, zones),
    hasItemMarkers: markers.length > 0
  };
}

function mapParkItemToMarker(
  item: ParkItem,
  zones: readonly ParkZone[]
): ParkItemsMapMarkerViewModel {
  const zoneName: string | null = resolveZoneName(item.zoneId ?? null, zones);
  const details: string[] = [
    item.type,
    zoneName ? `${zoneName}` : ''
  ].filter((value: string) => value.trim().length > 0);

  return {
    id: item.id ?? `${item.name}-${item.latitude}-${item.longitude}`,
    lat: item.latitude,
    lng: item.longitude,
    title: item.name,
    subtitle: item.category,
    details,
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
