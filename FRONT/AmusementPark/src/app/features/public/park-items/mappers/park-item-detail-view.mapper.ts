import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  resolveParkItemRichDescription
} from '@shared/utils/display/park-item-presentation.helpers';
import {
  ParkItemDetailRowViewModel,
  ParkItemDetailSpecGroupViewModel,
  ParkItemDetailViewModel,
  ParkItemLocationPointViewModel,
  ParkItemPhotoViewModel
} from '../models/park-item-detail-view.model';
import { buildAccessConditions } from './park-item-detail-access.mapper';
import { trimOrNull } from './park-item-detail-formatters';
import { buildLocationPoints, buildMapMarkers, resolveMapCenter } from './park-item-detail-location.mapper';
import {
  buildCategoryNavigation,
  buildItemsLink,
  buildParkLink,
  buildSearchNavigation,
  buildTypeNavigation,
  buildZoneNavigation
} from './park-item-detail-navigation.mapper';
import { buildPhotoCategories, buildPhotos } from './park-item-detail-photos.mapper';
import { resolveParkItemTypeIconClass, resolveParkItemTypeTone } from './park-item-detail-presentation.mapper';
import { buildRelatedItems } from './park-item-detail-related.mapper';
import {
  buildExperienceRows,
  buildPerformanceRows,
  buildSpecGroups,
  buildSpotlightRows,
  buildSummaryRows,
  buildTechnicalRows
} from './park-item-detail-rows.mapper';

export function mapParkItemToDetailViewModel(
  item: ParkItem | null,
  park: Park | null,
  manufacturerName: string | null,
  zoneName: string | null,
  currentLanguage: string,
  relatedItems: ParkItem[] = [],
  photos: ImageDto[] = [],
  imageTags: ImageTagDto[] = []
): ParkItemDetailViewModel | null {
  if (!item) {
    return null;
  }

  const technicalRows: ParkItemDetailRowViewModel[] = buildTechnicalRows(item, manufacturerName, currentLanguage);
  const performanceRows: ParkItemDetailRowViewModel[] = buildPerformanceRows(item, currentLanguage);
  const experienceRows: ParkItemDetailRowViewModel[] = buildExperienceRows(item, currentLanguage);
  const locationPoints: ParkItemLocationPointViewModel[] = buildLocationPoints(item, currentLanguage);
  const specGroups: ParkItemDetailSpecGroupViewModel[] = buildSpecGroups(technicalRows, performanceRows, experienceRows);
  if (locationPoints.length === 0) {
    specGroups.push(buildNoGeolocationSpecGroup());
  }

  const hasPreciseLocations: boolean = locationPoints.some((point: ParkItemLocationPointViewModel) => !point.isGeneralFallback);
  const galleryPhotos: ParkItemPhotoViewModel[] = buildPhotos(photos, imageTags);
  const itemsLink: string[] | null = buildItemsLink(park, currentLanguage);
  const parkLink: string[] | null = buildParkLink(park, currentLanguage);

  return {
    name: item.name?.trim() ?? '',
    categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
    typeLabelKey: getParkItemTypeTranslationKey(item.type),
    typeIconClass: resolveParkItemTypeIconClass(item.type),
    typeTone: resolveParkItemTypeTone(item.type, item.category),
    parkName: park?.name?.trim() ?? null,
    homeLink: ['/', currentLanguage, 'home'],
    parkLink,
    itemsLink,
    categoryNavigation: buildCategoryNavigation(itemsLink, item.category),
    typeNavigation: buildTypeNavigation(itemsLink, item.type),
    subtypeNavigation: buildSearchNavigation(itemsLink, item.subtype),
    zoneNavigation: buildZoneNavigation(itemsLink, item.zoneId),
    description: resolveParkItemRichDescription(item, currentLanguage),
    manufacturerName,
    modelName: trimOrNull(item.attractionDetails?.model),
    status: trimOrNull(item.attractionDetails?.status),
    zoneName,
    subtype: trimOrNull(item.subtype),
    spotlightRows: buildSpotlightRows(item, performanceRows),
    summaryRows: buildSummaryRows(item, park, manufacturerName, zoneName, currentLanguage),
    specGroups,
    photos: galleryPhotos,
    photoCategories: buildPhotoCategories(galleryPhotos),
    heroPhoto: galleryPhotos.find((photo: ParkItemPhotoViewModel) => photo.isCurrent) ?? galleryPhotos[0] ?? null,
    accessConditions: buildAccessConditions(item, currentLanguage),
    locationPoints,
    mapMarkers: buildMapMarkers(locationPoints, item.name),
    mapCenter: resolveMapCenter(locationPoints, item, park),
    mapZoom: locationPoints.length > 1 ? 17 : 18,
    hasPreciseLocations,
    relatedItems: buildRelatedItems(item, park, relatedItems, currentLanguage, zoneName)
  };
}

function buildNoGeolocationSpecGroup(): ParkItemDetailSpecGroupViewModel {
  return {
    titleKey: 'parkItems.detail.locationTitle',
    iconClass: 'pi pi-map-marker',
    rows: [{
      labelKey: 'parkItems.fields.coordinates',
      value: '',
      valueKey: 'parkItems.detail.noGeolocationMessage',
      iconClass: 'pi pi-map-marker',
      isTextualValue: true
    }]
  };
}
