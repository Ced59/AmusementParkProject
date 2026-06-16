import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkType } from '@app/models/parks/park-type';
import { buildParkAddressLine, buildParkLocationLine } from '@shared/utils/display/park-presentation.helpers';
import { getParkTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { buildPublicParkImagesRouteCommands, buildPublicParkItemsRouteCommands, buildPublicParkMapRouteCommands, buildPublicParkZonesRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { ParkDetailInfoRowViewModel } from '../models/park-detail-info-row.model';
import {
  ParkDetailPhotoViewModel,
  ParkDetailStatViewModel,
  ParkDetailViewModel
} from '../models/park-detail-view.model';
import { buildPhotoCategories, buildPhotos, resolveParkHeroImageId } from './park-detail-gallery.mapper';
import {
  buildIdentityRows,
  buildLocationRows,
  buildPracticalRows,
  buildPublicationRows,
  buildStats,
  normalizeOptionalString
} from './park-detail-info.mapper';
import { ParkDetailItemPhotoSource, ParkDetailReferenceNames, ParkDetailStatsSource } from './park-detail-mapping.model';

export type { ParkDetailItemPhotoSource, ParkDetailReferenceNames, ParkDetailStatsSource } from './park-detail-mapping.model';

export function mapParkToDetailViewModel(
  park: Park,
  currentLanguage: string,
  references: ParkDetailReferenceNames = {},
  statsSource: ParkDetailStatsSource = {},
  parkPhotos: ImageDto[] = [],
  itemPhotoSources: ParkDetailItemPhotoSource[] = [],
  imageTags: ImageTagDto[] = []
): ParkDetailViewModel {
  const hasLocationInfo: boolean = Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  const websiteUrl: string | null = normalizeOptionalString(park.webSiteUrl);
  const countryCode: string | null = normalizeOptionalString(park.countryCode);
  const countryName: string | null = normalizeOptionalString(references.countryName) ?? countryCode;
  const city: string | null = normalizeOptionalString(park.city);
  const street: string | null = normalizeOptionalString(park.street);
  const postalCode: string | null = normalizeOptionalString(park.postalCode);
  const addressLine: string | null = buildParkAddressLine(park);
  const locationLine: string | null = buildParkLocationLine(park, countryName);
  const logoImageId: string | null = normalizeOptionalString(park.currentLogoImageId);
  const type: ParkType | null = park.type ?? null;
  const founderId: string | null = normalizeOptionalString(park.founderId);
  const operatorId: string | null = normalizeOptionalString(park.operatorId);
  const founderName: string | null = normalizeOptionalString(references.founderName) ?? founderId;
  const operatorName: string | null = normalizeOptionalString(references.operatorName) ?? operatorId;
  const isVisible: boolean | null = park.isVisible ?? null;
  const isFeaturedOnHome: boolean | null = park.isFeaturedOnHome ?? null;
  const featuredHomeOrder: number | null = park.featuredHomeOrder ?? null;
  const isFeaturedOnHomeSponsored: boolean | null = park.isFeaturedOnHomeSponsored ?? null;
  const hasPracticalInfo: boolean = !!countryName || !!city || !!street || !!postalCode || !!websiteUrl || !!founderId || !!operatorId;
  const hasIdentity: boolean = !!park.id && !!park.name;
  const description: string | null = normalizeOptionalString(resolveLocalizedValue(park.descriptions, currentLanguage) ?? null);
  const totalItems: number = statsSource.totalItems ?? 0;
  const zoneCount: number = statsSource.zoneCount ?? 0;

  const identityRows: ParkDetailInfoRowViewModel[] = buildIdentityRows(park.id ?? null, type, founderName, operatorName, founderId, operatorId);
  const practicalRows: ParkDetailInfoRowViewModel[] = buildPracticalRows(
    countryName,
    city,
    postalCode,
    street,
    addressLine,
    websiteUrl,
    founderId,
    founderName,
    operatorId,
    operatorName,
    currentLanguage
  );
  const publicationRows: ParkDetailInfoRowViewModel[] = buildPublicationRows(
    isVisible,
    isFeaturedOnHome,
    isFeaturedOnHomeSponsored,
    featuredHomeOrder,
    logoImageId,
    currentLanguage
  );
  const locationRows: ParkDetailInfoRowViewModel[] = buildLocationRows(hasLocationInfo, park.latitude, park.longitude);
  const stats: ParkDetailStatViewModel[] = buildStats(totalItems, zoneCount, countryName, type);
  const photos: ParkDetailPhotoViewModel[] = buildPhotos(park, parkPhotos, itemPhotoSources, imageTags, currentLanguage);
  const primaryPhoto: ParkDetailPhotoViewModel | null = photos[0] ?? null;
  const heroImageId: string | null = resolveParkHeroImageId(parkPhotos) ?? logoImageId;

  return {
    id: park.id ?? null,
    name: park.name?.trim() ?? '',
    countryCode,
    countryName,
    city,
    street,
    postalCode,
    websiteUrl,
    logoImageId,
    heroImageId,
    description,
    type,
    typeLabelKey: type ? getParkTypeTranslationKey(type) : null,
    founderId,
    founderName,
    operatorId,
    operatorName,
    isVisible,
    isFeaturedOnHome,
    featuredHomeOrder,
    isFeaturedOnHomeSponsored,
    locationLine,
    addressLine,
    latitude: hasLocationInfo ? park.latitude : null,
    longitude: hasLocationInfo ? park.longitude : null,
    hasPracticalInfo,
    hasLocationInfo,
    hasDescription: !!description,
    exploreLink: hasIdentity
      ? buildPublicParkItemsRouteCommands({ language: currentLanguage, parkId: park.id, parkName: park.name })
      : null,
    zonesLink: hasIdentity && zoneCount > 0
      ? buildPublicParkZonesRouteCommands({ language: currentLanguage, parkId: park.id, parkName: park.name })
      : null,
    imagesLink: hasIdentity && primaryPhoto
      ? buildPublicParkImagesRouteCommands({ language: currentLanguage, parkId: park.id, parkName: park.name })
      : null,
    mapLink: hasIdentity
      ? buildPublicParkMapRouteCommands({ language: currentLanguage, parkId: park.id, parkName: park.name })
      : null,
    primaryPhoto,
    identityRows,
    practicalRows,
    publicationRows,
    locationRows,
    stats,
    photos,
    photoCategories: buildPhotoCategories(photos),
  };
}
