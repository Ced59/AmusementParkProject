import { ParkMapItem, ParkMapItems, ParkMapUnlocatedItem, ParkMapZone } from '@app/models/parks/park-map-items';
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

export function mapParkMapItemsToViewModel(response: ParkMapItems, language: string): ParkItemsMapViewModel {
  const markers: ParkItemsMapMarkerViewModel[] = response.items
    .filter((item: ParkMapItem) => hasValidPosition(item.latitude, item.longitude))
    .map((item: ParkMapItem) => mapItemToMarker(response, item, language))
    .sort((left: ParkItemsMapMarkerViewModel, right: ParkItemsMapMarkerViewModel) => left.title?.localeCompare(right.title ?? '') ?? 0);

  return {
    parkId: response.park.id ?? null,
    parkName: response.park.name ?? null,
    language,
    center: resolveCenter(response, markers),
    markers,
    unlocatedItems: mapUnlocatedItems(response, language),
    categoryFilters: buildCategoryFilters(markers),
    zoneFilters: buildZoneFilters(markers, response.zones ?? []),
    hasItemMarkers: markers.length > 0
  };
}

function mapUnlocatedItems(response: ParkMapItems, language: string): ParkItemsMapUnlocatedItemViewModel[] {
  return (response.unlocatedItems ?? [])
    .map((item: ParkMapUnlocatedItem) => ({
      id: item.id ?? null,
      name: item.name,
      categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
      typeLabelKey: getParkItemTypeTranslationKey(item.type),
      detailLink: buildPublicParkItemRouteCommands({
        language,
        parkId: response.park.id,
        parkName: response.park.name,
        itemId: item.id,
        itemName: item.name
      })
    }))
    .sort((left: ParkItemsMapUnlocatedItemViewModel, right: ParkItemsMapUnlocatedItemViewModel) => left.name.localeCompare(right.name));
}

function mapItemToMarker(response: ParkMapItems, item: ParkMapItem, language: string): ParkItemsMapMarkerViewModel {
  const zoneName: string | null = resolveZoneName(item.zoneId ?? null, response.zones ?? []);
  const details: string[] = zoneName ? [zoneName] : [];

  return {
    id: item.id,
    itemId: item.id,
    itemName: item.name,
    lat: item.latitude,
    lng: item.longitude,
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
    detailTranslationKeys: [getParkItemTypeTranslationKey(item.type)],
    detailActionRouteCommands: buildParkItemMapDetailRouteCommands({
      language,
      parkId: response.park.id,
      parkName: response.park.name,
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

function buildZoneFilters(markers: readonly ParkItemsMapMarkerViewModel[], zones: readonly ParkMapZone[]): ParkItemsMapFilterOptionViewModel[] {
  const countsByZoneId: Map<string, number> = new Map<string, number>();

  for (const marker of markers) {
    if (marker.zoneId) {
      countsByZoneId.set(marker.zoneId, (countsByZoneId.get(marker.zoneId) ?? 0) + 1);
    }
  }

  return zones
    .filter((zone: ParkMapZone) => countsByZoneId.has(zone.id))
    .sort((left: ParkMapZone, right: ParkMapZone) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0) || left.name.localeCompare(right.name))
    .map((zone: ParkMapZone) => ({
      key: zone.id,
      labelText: zone.name,
      iconClass: 'pi pi-map-marker',
      count: countsByZoneId.get(zone.id) ?? 0
    }));
}

function resolveCenter(response: ParkMapItems, markers: readonly ParkItemsMapMarkerViewModel[]): [number, number] {
  if (markers.length > 0) {
    return [markers[0].lat, markers[0].lng];
  }

  const parkLatitude: number | null | undefined = response.park.latitude;
  const parkLongitude: number | null | undefined = response.park.longitude;
  if (hasValidPosition(parkLatitude, parkLongitude)) {
    return [parkLatitude!, parkLongitude!];
  }

  return [0, 0];
}

function resolveZoneName(zoneId: string | null, zones: readonly ParkMapZone[]): string | null {
  if (!zoneId) {
    return null;
  }

  return zones.find((zone: ParkMapZone) => zone.id === zoneId)?.name?.trim() || null;
}

function hasValidPosition(latitude: number | null | undefined, longitude: number | null | undefined): boolean {
  return latitude !== null
    && latitude !== undefined
    && longitude !== null
    && longitude !== undefined
    && Number.isFinite(latitude)
    && Number.isFinite(longitude)
    && Math.abs(latitude) <= 90
    && Math.abs(longitude) <= 180
    && !(latitude === 0 && longitude === 0);
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
