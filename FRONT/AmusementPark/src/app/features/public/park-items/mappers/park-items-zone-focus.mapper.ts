import { ImageDto } from '@app/models/images/image-dto';
import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerBucket } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { getParkItemCategoryTranslationKey, getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { resolveParkItemMarkerIconKind } from '@shared/utils/maps/map-marker-icon-kind.resolver';
import { buildParkItemMapDetailRouteCommands } from '@shared/services/maps/map-marker-detail-route.helpers';
import { ParkItemsCountTagViewModel } from '../models/park-items-page-view.model';
import { ParkItemsMapViewModel, ParkItemsZoneFocusViewModel } from '../models/park-items-zone-focus.model';

export function mapParkItemsZoneFocusViewModel(
  park: Park | null,
  explorer: ParkExplorer | null,
  zones: readonly ParkZone[],
  displayedItems: readonly ParkItem[],
  typeFilterScopeItems: readonly ParkItem[],
  selectedZoneId: string | null,
  parkPhotos: readonly ImageDto[],
  currentLanguage: string
): ParkItemsZoneFocusViewModel | null {
  if (!park) {
    return null;
  }

  const activeZone: ParkZone | null = selectedZoneId
    ? zones.find((zone: ParkZone) => zone.id === selectedZoneId) ?? null
    : null;
  const activeZoneBucket: ParkExplorerBucket | null = activeZone?.id
    ? explorer?.zones.find((bucket: ParkExplorerBucket) => bucket.id === activeZone.id) ?? null
    : null;
  const zoneName: string = resolveZoneName(activeZone, currentLanguage) ?? park.name ?? '';
  const zoneDescription: string | null = resolveZoneDescription(activeZone, currentLanguage);
  const heroImageId: string | null = resolveParkHeroImageId(parkPhotos);
  const map: ParkItemsMapViewModel = mapDisplayedItemsToMapViewModel(park, displayedItems, zones, currentLanguage);
  const topTypeHighlights: ParkItemsCountTagViewModel[] = buildTypeHighlightsFromItems(typeFilterScopeItems);

  return {
    parkName: park.name ?? '',
    zoneId: activeZone?.id ?? null,
    zoneName,
    zoneDescription,
    heroImageId,
    totalItems: activeZoneBucket?.totalItems ?? displayedItems.length,
    displayedItems: displayedItems.length,
    hasActiveZone: !!activeZone,
    topTypeHighlights,
    map
  };
}

function mapDisplayedItemsToMapViewModel(
  park: Park,
  displayedItems: readonly ParkItem[],
  zones: readonly ParkZone[],
  currentLanguage: string
): ParkItemsMapViewModel {
  const markers = displayedItems
    .filter((item: ParkItem) => item.isVisible !== false)
    .filter((item: ParkItem) => hasValidParkItemPosition(item))
    .map((item: ParkItem) => {
      const zoneName: string | null = resolveZoneNameById(item.zoneId ?? null, zones, currentLanguage);
      const details: string[] = [zoneName].filter((value: string | null | undefined): value is string => !!value && value.trim().length > 0);
      const detailTranslationKeys: string[] = normalizeOptionalString(item.type)
        ? [getParkItemTypeTranslationKey(item.type)]
        : [];

      return {
        id: item.id ?? `${item.name}-${item.latitude}-${item.longitude}`,
        lat: item.latitude!,
        lng: item.longitude!,
        title: item.name,
        subtitle: item.category,
        subtitleTranslationKey: getParkItemCategoryTranslationKey(item.category),
        details,
        detailTranslationKeys,
        directionsActionEnabled: true,
        iconKind: resolveParkItemMarkerIconKind({
          category: item.category,
          type: item.type,
          subtype: item.subtype ?? null
        }),
        detailActionRouteCommands: buildParkItemMapDetailRouteCommands({
          language: currentLanguage,
          parkId: park.id,
          parkName: park.name,
          itemId: item.id,
          itemName: item.name
        })
      };
    })
    .sort((left, right) => (left.title ?? '').localeCompare(right.title ?? ''));

  return {
    center: resolveMapCenter(park, markers),
    markers,
    hasMarkers: markers.length > 0
  };
}

function resolveMapCenter(park: Park, markers: readonly { lat: number; lng: number }[]): [number, number] {
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

function resolveZoneName(zone: ParkZone | null, currentLanguage: string): string | null {
  if (!zone) {
    return null;
  }

  const localizedName: string | undefined = resolveLocalizedValue(zone.names, currentLanguage);
  return normalizeOptionalString(localizedName) ?? normalizeOptionalString(zone.name) ?? normalizeOptionalString(zone.id);
}

function resolveZoneDescription(zone: ParkZone | null, currentLanguage: string): string | null {
  if (!zone) {
    return null;
  }

  const localizedDescription: string | undefined = resolveLocalizedValue(zone.descriptions, currentLanguage);
  return normalizeOptionalString(localizedDescription);
}

function resolveZoneNameById(zoneId: string | null, zones: readonly ParkZone[], currentLanguage: string): string | null {
  if (!zoneId) {
    return null;
  }

  return resolveZoneName(zones.find((zone: ParkZone) => zone.id === zoneId) ?? null, currentLanguage);
}

function buildTypeHighlightsFromItems(items: readonly ParkItem[]): ParkItemsCountTagViewModel[] {
  const countsByType: Map<string, number> = new Map<string, number>();

  for (const item of items) {
    if (!item.type) {
      continue;
    }

    countsByType.set(item.type, (countsByType.get(item.type) ?? 0) + 1);
  }

  return Array.from(countsByType.entries())
    .map(([type, count]: [string, number]) => ({
      value: type,
      labelKey: getParkItemTypeTranslationKey(type),
      count
    }))
    .sort((left: ParkItemsCountTagViewModel, right: ParkItemsCountTagViewModel) => {
      return right.count - left.count || left.value.localeCompare(right.value);
    });
}

function resolveParkHeroImageId(parkPhotos: readonly ImageDto[]): string | null {
  const currentPhoto: ImageDto | undefined = parkPhotos.find((photo: ImageDto) => {
    return photo.isPublished !== false && photo.isCurrent && normalizeOptionalString(photo.id) !== null;
  });

  if (currentPhoto) {
    return normalizeOptionalString(currentPhoto.id);
  }

  const fallbackPhoto: ImageDto | undefined = parkPhotos.find((photo: ImageDto) => {
    return photo.isPublished !== false && normalizeOptionalString(photo.id) !== null;
  });

  return normalizeOptionalString(fallbackPhoto?.id);
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
