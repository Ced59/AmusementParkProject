import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerBucket, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import {
  ParkItemsCountTagViewModel,
  ParkItemsPageViewModel,
  ParkItemZoneCardViewModel
} from '../models/park-items-page-view.model';

export function mapParkItemsPageViewModel(
  park: Park | null,
  explorer: ParkExplorer | null,
  currentLanguage: string,
  totalResults: number,
  activeZoneLabel: string | null,
  hasZones: boolean
): ParkItemsPageViewModel | null {
  if (!park || !explorer) {
    return null;
  }

  return {
    parkName: park.name?.trim() ?? '',
    backLink: buildParkLink(park, currentLanguage),
    totalItems: explorer.overview.totalItems,
    totalResults,
    zoneCount: explorer.zones.filter((bucket: ParkExplorerBucket) => bucket.totalItems > 0).length,
    hasZones,
    activeZoneLabel,
    topTypeHighlights: mapCountTags(explorer.overview.countsByType, 4)
  };
}

export function mapParkExplorerBucketToZoneCardViewModel(
  bucket: ParkExplorerBucket,
  currentLanguage: string,
  selectedZoneId: string | null
): ParkItemZoneCardViewModel {
  return {
    id: bucket.id ?? null,
    name: resolveLocalizedValue(bucket.names, currentLanguage) ?? bucket.name,
    totalItems: bucket.totalItems,
    typeHighlights: mapCountTags(bucket.countsByType, 3),
    isSelected: selectedZoneId === (bucket.id ?? null)
  };
}

function mapCountTags(counts: ParkExplorerCount[], maxCount: number): ParkItemsCountTagViewModel[] {
  return [...counts]
    .sort((left: ParkExplorerCount, right: ParkExplorerCount) => right.count - left.count)
    .slice(0, maxCount)
    .map((item: ParkExplorerCount) => ({
      labelKey: getParkItemTypeTranslationKey(item.key),
      count: item.count
    }));
}

function buildParkLink(park: Park, currentLanguage: string): string[] | null {
  if (!park.id || !park.name) {
    return null;
  }

  return ['/', currentLanguage, 'park', park.id, buildParkSlug(park.name)];
}
