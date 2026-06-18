import { ImageDto } from '@app/models/images/image-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { MeasurementSystem, DEFAULT_MEASUREMENT_SYSTEM } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
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
  buildImagesLink,
  buildItemsLink,
  buildParkLink,
  buildSearchNavigation,
  buildTypeNavigation,
  buildVideosLink,
  buildZoneNavigation
} from './park-item-detail-navigation.mapper';
import { buildPhotos } from './park-item-detail-photos.mapper';
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
  textTruncator: NaturalTextTruncatorService | null = null,
  measurementSystem: MeasurementSystem = DEFAULT_MEASUREMENT_SYSTEM,
  measurementConversionService: MeasurementConversionService = new MeasurementConversionService()
): ParkItemDetailViewModel | null {
  if (!item) {
    return null;
  }

  const technicalRows: ParkItemDetailRowViewModel[] = buildTechnicalRows(item, manufacturerName, currentLanguage);
  const performanceRows: ParkItemDetailRowViewModel[] = buildPerformanceRows(
    item,
    currentLanguage,
    measurementSystem,
    measurementConversionService
  );
  const experienceRows: ParkItemDetailRowViewModel[] = buildExperienceRows(item, currentLanguage);
  const locationPoints: ParkItemLocationPointViewModel[] = buildLocationPoints(item, currentLanguage);
  const specGroups: ParkItemDetailSpecGroupViewModel[] = buildSpecGroups(technicalRows, performanceRows, experienceRows);
  if (locationPoints.length === 0) {
    specGroups.push(buildNoGeolocationSpecGroup());
  }

  const hasPreciseLocations: boolean = locationPoints.some((point: ParkItemLocationPointViewModel) => !point.isGeneralFallback);
  const galleryPhotos: ParkItemPhotoViewModel[] = buildPhotos(photos, []);
  const heroPhoto: ParkItemPhotoViewModel | null = galleryPhotos.find((photo: ParkItemPhotoViewModel) => photo.isCurrent) ?? galleryPhotos[0] ?? null;
  const itemsLink: string[] | null = buildItemsLink(park, currentLanguage);
  const parkLink: string[] | null = buildParkLink(park, currentLanguage);
  const imagesLink: string[] | null = heroPhoto ? buildImagesLink(park, item, currentLanguage) : null;
  const videosLink: string[] | null = buildVideosLink(park, item, currentLanguage);

  return {
    id: item.id ?? null,
    name: item.name?.trim() ?? '',
    categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
    typeLabelKey: getParkItemTypeTranslationKey(item.type),
    typeIconClass: resolveParkItemTypeIconClass(item.type),
    typeTone: resolveParkItemTypeTone(item.type, item.category),
    parkName: park?.name?.trim() ?? null,
    homeLink: ['/', currentLanguage, 'home'],
    parkLink,
    itemsLink,
    imagesLink,
    videosLink,
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
    heroPhoto,
    accessConditions: buildAccessConditions(item, currentLanguage, measurementSystem, measurementConversionService),
    locationPoints,
    mapMarkers: buildMapMarkers(locationPoints, item.name),
    mapCenter: resolveMapCenter(locationPoints, item, park),
    mapZoom: locationPoints.length > 1 ? 17 : 18,
    hasPreciseLocations,
    relatedItems: buildRelatedItems(
      item,
      park,
      relatedItems,
      currentLanguage,
      zoneName,
      textTruncator,
      measurementSystem,
      measurementConversionService
    )
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
