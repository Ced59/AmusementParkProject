import { ParkExplorerBucket, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { ParkZone } from '@app/models/parks/park-zone';
import { getParkItemCategoryTranslationKey, getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { ParkDetailInfoRowViewModel } from '../models/park-detail-info-row.model';
import { ParkZoneDetailCountViewModel, ParkZoneDetailViewModel } from '../models/park-zone-detail-view.model';

export function mapParkZoneToDetailViewModel(
  zone: ParkZone,
  bucket: ParkExplorerBucket | null,
  itemsLink: string[] | null,
  currentLanguage: string
): ParkZoneDetailViewModel {
  const localizedName: string | undefined = resolveLocalizedValue(zone.names, currentLanguage);
  const localizedDescription: string | undefined = resolveLocalizedValue(zone.descriptions, currentLanguage);
  const zoneId: string | null = zone.id ?? null;
  const latitude: number | null = Number.isFinite(zone.latitude ?? Number.NaN) ? zone.latitude ?? null : null;
  const longitude: number | null = Number.isFinite(zone.longitude ?? Number.NaN) ? zone.longitude ?? null : null;
  const topCounts: ParkZoneDetailCountViewModel[] = buildTopCounts(bucket);
  const infoRows: ParkDetailInfoRowViewModel[] = buildZoneInfoRows(zone, latitude, longitude);

  return {
    id: zoneId,
    name: normalizeOptionalString(localizedName) ?? normalizeOptionalString(zone.name) ?? zoneId ?? '',
    slug: normalizeOptionalString(zone.slug),
    description: normalizeOptionalString(localizedDescription),
    totalItems: bucket?.totalItems ?? 0,
    topCounts,
    latitude,
    longitude,
    isVisible: zone.isVisible ?? true,
    sortOrder: zone.sortOrder ?? null,
    itemsLink,
    queryParams: zoneId ? { zone: zoneId } : null,
    infoRows,
  };
}

function buildTopCounts(bucket: ParkExplorerBucket | null): ParkZoneDetailCountViewModel[] {
  if (!bucket) {
    return [];
  }

  const categoryCounts: ParkZoneDetailCountViewModel[] = bucket.countsByCategory
    .filter((count: ParkExplorerCount) => count.count > 0)
    .map((count: ParkExplorerCount) => ({
      labelKey: getParkItemCategoryTranslationKey(count.key),
      count: count.count,
      icon: 'pi pi-tag'
    }));

  const typeCounts: ParkZoneDetailCountViewModel[] = bucket.countsByType
    .filter((count: ParkExplorerCount) => count.count > 0)
    .map((count: ParkExplorerCount) => ({
      labelKey: getParkItemTypeTranslationKey(count.key),
      count: count.count,
      icon: 'pi pi-sparkles'
    }));

  return [...categoryCounts, ...typeCounts]
    .sort((left: ParkZoneDetailCountViewModel, right: ParkZoneDetailCountViewModel) => right.count - left.count)
    .slice(0, 4);
}

function buildZoneInfoRows(zone: ParkZone, latitude: number | null, longitude: number | null): ParkDetailInfoRowViewModel[] {
  const rows: ParkDetailInfoRowViewModel[] = [];

  if (zone.id) {
    rows.push({ labelKey: 'parks.detail.identity.reference', value: zone.id, iconClass: 'pi pi-hashtag', isMonospace: true });
  }

  if (zone.slug) {
    rows.push({ labelKey: 'parks.detail.zones.slug', value: zone.slug, iconClass: 'pi pi-link' });
  }

  if (zone.sortOrder != null) {
    rows.push({ labelKey: 'parks.detail.zones.sortOrder', value: zone.sortOrder, iconClass: 'pi pi-sort-amount-down' });
  }

  if (latitude != null && longitude != null) {
    rows.push({ labelKey: 'parks.fields.coordinates', value: `${latitude}, ${longitude}`, iconClass: 'pi pi-map-marker' });
  }

  return rows;
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
