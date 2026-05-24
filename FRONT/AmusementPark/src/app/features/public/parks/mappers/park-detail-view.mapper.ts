import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkType } from '@app/models/parks/park-type';
import { buildParkAddressLine, buildParkLocationLine } from '@shared/utils/display/park-presentation.helpers';
import { getLocalizedBooleanDisplay, getParkTypeTranslationKey, normalizeTranslationSegment } from '@shared/utils/display/display-label.helpers';
import { getParkItemCategoryTranslationKey } from '@shared/utils/display/park-item-presentation.helpers';
import { resolveLocalizedValue } from '@shared/utils/localization';
import {
  buildPublicParkItemRouteCommands,
  buildPublicParkItemsRouteCommands,
  buildPublicParkReferenceRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';
import { ParkDetailInfoRowViewModel } from '../models/park-detail-info-row.model';
import {
  ParkDetailPhotoCategoryOptionViewModel,
  ParkDetailPhotoViewModel,
  ParkDetailStatViewModel,
  ParkDetailViewModel
} from '../models/park-detail-view.model';

export interface ParkDetailReferenceNames {
  founderName?: string | null;
  operatorName?: string | null;
  countryName?: string | null;
}

export interface ParkDetailStatsSource {
  totalItems?: number | null;
  zoneCount?: number | null;
}

export interface ParkDetailItemPhotoSource {
  item: ParkItem;
  photos: ImageDto[];
}

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
  const heroImageId: string | null = resolveParkHeroImageId(parkPhotos);

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
    identityRows,
    practicalRows,
    publicationRows,
    locationRows,
    stats,
    photos,
    photoCategories: buildPhotoCategories(photos),
  };
}


function resolveParkHeroImageId(parkPhotos: ImageDto[]): string | null {

  const currentPhoto: ImageDto | undefined = parkPhotos.find((photo: ImageDto) => {
    return photo.isCurrent && isDisplayableParkPhoto(photo) && normalizeOptionalString(photo.id) !== null;
  });

  if (currentPhoto) {
    const currentImageId: string | null = normalizeOptionalString(currentPhoto.id);
    return currentImageId;
  }

  const fallbackPhoto: ImageDto | undefined = parkPhotos.find((photo: ImageDto) => {
    return isDisplayableParkPhoto(photo) && normalizeOptionalString(photo.id) !== null;
  });

  const fallbackImageId: string | null = normalizeOptionalString(fallbackPhoto?.id);
  return fallbackImageId;
}

function buildIdentityRows(
  parkId: string | null,
  type: ParkType | null,
  founderName: string | null,
  operatorName: string | null,
  founderId: string | null,
  operatorId: string | null
): ParkDetailInfoRowViewModel[] {
  const rows: ParkDetailInfoRowViewModel[] = [];

  if (type) {
    rows.push({
      labelKey: 'parks.detail.identity.type',
      value: null,
      valueKey: getParkTypeTranslationKey(type),
      iconClass: 'pi pi-tag'
    });
  }

  if (founderName) {
    rows.push({
      labelKey: 'parks.detail.identity.founder',
      value: founderName,
      iconClass: 'pi pi-user',
      isMonospace: founderName === founderId
    });
  }

  if (operatorName) {
    rows.push({
      labelKey: 'parks.detail.identity.operator',
      value: operatorName,
      iconClass: 'pi pi-briefcase',
      isMonospace: operatorName === operatorId
    });
  }

  if (parkId) {
    rows.push({
      labelKey: 'parks.detail.identity.reference',
      value: parkId,
      iconClass: 'pi pi-hashtag',
      isMonospace: true
    });
  }

  return rows;
}

function buildPracticalRows(
  countryName: string | null,
  city: string | null,
  postalCode: string | null,
  street: string | null,
  addressLine: string | null,
  websiteUrl: string | null,
  founderId: string | null,
  founderName: string | null,
  operatorId: string | null,
  operatorName: string | null,
  currentLanguage: string
): ParkDetailInfoRowViewModel[] {
  const rows: ParkDetailInfoRowViewModel[] = [];

  if (countryName) {
    rows.push({ labelKey: 'parks.fields.country', value: countryName, iconClass: 'pi pi-flag' });
  }

  if (city) {
    rows.push({ labelKey: 'parks.fields.city', value: city, iconClass: 'pi pi-building' });
  }

  if (postalCode) {
    rows.push({ labelKey: 'parks.detail.practical.postalCode', value: postalCode, iconClass: 'pi pi-inbox' });
  }

  if (street) {
    rows.push({ labelKey: 'parks.detail.practical.street', value: street, iconClass: 'pi pi-map-marker' });
  }

  if (addressLine && addressLine !== street) {
    rows.push({ labelKey: 'parks.fields.address', value: addressLine, iconClass: 'pi pi-map' });
  }

  if (websiteUrl) {
    rows.push({
      labelKey: 'parks.fields.website',
      value: websiteUrl,
      externalUrl: websiteUrl,
      iconClass: 'pi pi-external-link'
    });
  }

  if (operatorId) {
    const displayName: string = operatorName ?? operatorId;
    rows.push({
      labelKey: 'parks.detail.identity.operator',
      value: displayName,
      routerLink: buildPublicParkReferenceRouteCommands({
        language: currentLanguage,
        referenceId: operatorId,
        referenceName: displayName,
        kind: 'operator'
      }),
      iconClass: 'pi pi-briefcase',
      isMonospace: displayName === operatorId
    });
  }

  if (founderId) {
    const displayName: string = founderName ?? founderId;
    rows.push({
      labelKey: 'parks.detail.identity.founder',
      value: displayName,
      routerLink: buildPublicParkReferenceRouteCommands({
        language: currentLanguage,
        referenceId: founderId,
        referenceName: displayName,
        kind: 'founder'
      }),
      iconClass: 'pi pi-user',
      isMonospace: displayName === founderId
    });
  }

  return rows;
}

function buildPublicationRows(
  isVisible: boolean | null,
  isFeaturedOnHome: boolean | null,
  isFeaturedOnHomeSponsored: boolean | null,
  featuredHomeOrder: number | null,
  logoImageId: string | null,
  currentLanguage: string
): ParkDetailInfoRowViewModel[] {
  const rows: ParkDetailInfoRowViewModel[] = [];

  if (isVisible != null) {
    rows.push({
      labelKey: 'parks.detail.publication.visible',
      value: getLocalizedBooleanDisplay(isVisible, currentLanguage),
      iconClass: isVisible ? 'pi pi-eye' : 'pi pi-eye-slash'
    });
  }

  if (isFeaturedOnHome != null) {
    rows.push({
      labelKey: 'parks.detail.publication.featured',
      value: getLocalizedBooleanDisplay(isFeaturedOnHome, currentLanguage),
      iconClass: 'pi pi-star'
    });
  }

  if (featuredHomeOrder != null) {
    rows.push({
      labelKey: 'parks.detail.publication.featuredOrder',
      value: featuredHomeOrder,
      iconClass: 'pi pi-sort-numeric-down'
    });
  }

  if (isFeaturedOnHomeSponsored != null) {
    rows.push({
      labelKey: 'parks.detail.publication.sponsored',
      value: getLocalizedBooleanDisplay(isFeaturedOnHomeSponsored, currentLanguage),
      iconClass: 'pi pi-megaphone'
    });
  }

  if (logoImageId) {
    rows.push({
      labelKey: 'parks.detail.publication.logo',
      value: 'parks.detail.publication.logoAvailable',
      valueKey: 'parks.detail.publication.logoAvailable',
      iconClass: 'pi pi-image'
    });
  }

  return rows;
}

function buildLocationRows(hasLocationInfo: boolean, latitude: number, longitude: number): ParkDetailInfoRowViewModel[] {
  if (!hasLocationInfo) {
    return [];
  }

  return [
    { labelKey: 'parks.fields.latitude', value: latitude, iconClass: 'pi pi-compass' },
    { labelKey: 'parks.fields.longitude', value: longitude, iconClass: 'pi pi-compass' }
  ];
}

function buildStats(
  totalItems: number,
  zoneCount: number,
  countryName: string | null,
  type: ParkType | null
): ParkDetailStatViewModel[] {
  const stats: ParkDetailStatViewModel[] = [
    {
      labelKey: 'parkVisitor.summary.totalItems',
      value: totalItems,
      hintKey: 'parkVisitor.summary.viewAllItems',
      tone: 'primary'
    }
  ];

  if (zoneCount > 0) {
    stats.push({
      labelKey: 'parks.detail.stats.zones',
      value: zoneCount,
      hintKey: 'parks.detail.zones.title',
      tone: 'lime'
    });
  }

  if (countryName) {
    stats.push({
      labelKey: 'parks.fields.country',
      value: countryName,
      hintKey: 'parks.detail.practical.title',
      tone: 'sky'
    });
  }

  if (type) {
    stats.push({
      labelKey: 'parks.detail.identity.type',
      value: '•',
      hintKey: getParkTypeTranslationKey(type),
      tone: 'gold'
    });
  }

  return stats.slice(0, 3);
}


function buildPhotos(
  park: Park,
  parkPhotos: ImageDto[],
  itemPhotoSources: ParkDetailItemPhotoSource[],
  imageTags: ImageTagDto[],
  currentLanguage: string
): ParkDetailPhotoViewModel[] {
  const tagSlugById: Map<string, string> = new Map<string, string>(imageTags.map((tag: ImageTagDto) => [tag.id, tag.slug]));
  const mappedParkPhotos: ParkDetailPhotoViewModel[] = buildParkPhotos(park, parkPhotos, tagSlugById);
  const mappedItemPhotos: ParkDetailPhotoViewModel[] = itemPhotoSources.flatMap((source: ParkDetailItemPhotoSource) => {
    return buildItemPhotos(park, source.item, source.photos, currentLanguage);
  });


  return [...mappedParkPhotos, ...mappedItemPhotos]
    .filter((photo: ParkDetailPhotoViewModel) => photo.imageId.trim().length > 0)
    .sort((first: ParkDetailPhotoViewModel, second: ParkDetailPhotoViewModel) => {
      if (first.isCurrent !== second.isCurrent) {
        return first.isCurrent ? -1 : 1;
      }

      return getPhotoCategoryOrder(first.categoryKey) - getPhotoCategoryOrder(second.categoryKey);
    });
}

function buildParkPhotos(park: Park, photos: ImageDto[], tagSlugById: Map<string, string>): ParkDetailPhotoViewModel[] {
  const displayablePhotos: ImageDto[] = photos.filter((photo: ImageDto) => isDisplayableParkPhoto(photo));

  return displayablePhotos
    .map((photo: ImageDto) => {
      const firstKnownTag: string | undefined = photo.tagIds
        ?.map((tagId: string) => tagSlugById.get(tagId) ?? tagId)
        .find((tagSlug: string) => !!tagSlug);
      const categoryKey: string = resolveParkPhotoCategoryKey(firstKnownTag);
      const description: string | null = normalizeOptionalString(photo.description);
      const parkName: string = park.name?.trim() ?? 'Park';

      return {
        id: photo.id,
        imageId: photo.id,
        category: photo.category,
        categoryKey,
        categoryLabelKey: resolveParkPhotoCategoryLabelKey(categoryKey),
        description,
        alt: description ?? `${parkName} photo`,
        isCurrent: photo.isCurrent,
        sourceTitle: parkName,
        sourceIconClass: 'pi pi-map-marker',
        sourceRouterLink: null,
        sourceLinkLabelKey: null
      };
    });
}

function buildItemPhotos(
  park: Park,
  item: ParkItem,
  photos: ImageDto[],
  currentLanguage: string
): ParkDetailPhotoViewModel[] {
  const categoryKey: string = `item-${normalizeTranslationSegment(item.category, 'other')}`;
  const categoryLabelKey: string = getParkItemCategoryTranslationKey(item.category);
  const itemName: string = item.name?.trim() ?? '';
  const itemLink: string[] | null = buildParkItemLink(park, item, currentLanguage);

  if (!itemName) {
    return [];
  }

  return photos
    .filter((photo: ImageDto) => isDisplayableItemPhoto(photo))
    .map((photo: ImageDto) => {
      const description: string | null = normalizeOptionalString(photo.description);

      return {
        id: photo.id,
        imageId: photo.id,
        category: photo.category,
        categoryKey,
        categoryLabelKey,
        description,
        alt: description ?? `${itemName} photo`,
        isCurrent: photo.isCurrent,
        sourceTitle: itemName,
        sourceSubtitle: categoryLabelKey,
        sourceIconClass: resolveParkItemSourceIconClass(item.category),
        sourceRouterLink: itemLink,
        sourceLinkLabelKey: 'parks.photos.openItem'
      };
    });
}

function buildPhotoCategories(photos: ParkDetailPhotoViewModel[]): ParkDetailPhotoCategoryOptionViewModel[] {
  const countByCategoryKey: Map<string, { count: number; labelKey: string }> = new Map<string, { count: number; labelKey: string }>();

  for (const photo of photos) {
    const current = countByCategoryKey.get(photo.categoryKey);
    countByCategoryKey.set(photo.categoryKey, {
      count: (current?.count ?? 0) + 1,
      labelKey: photo.categoryLabelKey
    });
  }

  return Array.from(countByCategoryKey.entries())
    .map(([key, value]: [string, { count: number; labelKey: string }]) => ({
      key,
      labelKey: value.labelKey,
      count: value.count
    }))
    .sort((first: ParkDetailPhotoCategoryOptionViewModel, second: ParkDetailPhotoCategoryOptionViewModel) => {
      return getPhotoCategoryOrder(first.key) - getPhotoCategoryOrder(second.key);
    });
}

function isDisplayableParkPhoto(photo: ImageDto): boolean {
  return isOwnerGalleryCategory(photo.category) && (photo.isPublished !== false || photo.isCurrent);
}

function isDisplayableItemPhoto(photo: ImageDto): boolean {
  return !isAdministrativeOnlyImageCategory(photo.category) && (photo.isPublished !== false || photo.isCurrent);
}

function isOwnerGalleryCategory(category: ImageCategory | number | string | null | undefined): boolean {
  return !isAdministrativeOnlyImageCategory(category);
}

function isAdministrativeOnlyImageCategory(category: ImageCategory | number | string | null | undefined): boolean {
  const normalizedCategory: string = String(category ?? '').toUpperCase();
  return normalizedCategory === ImageCategory.AVATAR
    || normalizedCategory === ImageCategory.PARK_LOGO
    || normalizedCategory === '0'
    || normalizedCategory === '1';
}

function resolveParkPhotoCategoryKey(tagIdOrSlug: string | undefined): string {
  const normalizedValue: string = (tagIdOrSlug ?? '').toLowerCase();

  if (normalizedValue.includes('entrance')) {
    return 'park-entrance';
  }

  if (normalizedValue.includes('overview')) {
    return 'park-overview';
  }

  if (normalizedValue.includes('map')) {
    return 'park-map';
  }

  if (normalizedValue.includes('atmosphere') || normalizedValue.includes('atmosphère')) {
    return 'park-atmosphere';
  }

  if (normalizedValue.includes('event')) {
    return 'park-event';
  }

  if (normalizedValue.includes('halloween')) {
    return 'park-halloween';
  }

  if (normalizedValue.includes('christmas') || normalizedValue.includes('noel') || normalizedValue.includes('noël')) {
    return 'park-christmas';
  }

  if (normalizedValue.includes('easter') || normalizedValue.includes('paques') || normalizedValue.includes('pâques')) {
    return 'park-easter';
  }

  if (normalizedValue.includes('food')) {
    return 'park-food';
  }

  if (normalizedValue.includes('service')) {
    return 'park-services';
  }

  return 'park-gallery';
}

function resolveParkPhotoCategoryLabelKey(categoryKey: string): string {
  switch (categoryKey) {
    case 'park-entrance':
      return 'parks.photos.categories.entrance';
    case 'park-overview':
      return 'parks.photos.categories.overview';
    case 'park-map':
      return 'parks.photos.categories.map';
    case 'park-atmosphere':
      return 'parks.photos.categories.atmosphere';
    case 'park-event':
      return 'parks.photos.categories.event';
    case 'park-halloween':
      return 'parks.photos.categories.halloween';
    case 'park-christmas':
      return 'parks.photos.categories.christmas';
    case 'park-easter':
      return 'parks.photos.categories.easter';
    case 'park-food':
      return 'parks.photos.categories.food';
    case 'park-services':
      return 'parks.photos.categories.services';
    default:
      return 'parks.photos.categories.gallery';
  }
}

function getPhotoCategoryOrder(categoryKey: string): number {
  switch (categoryKey) {
    case 'park-gallery':
      return 0;
    case 'park-entrance':
      return 1;
    case 'park-overview':
      return 2;
    case 'park-map':
      return 3;
    case 'park-atmosphere':
      return 4;
    case 'park-event':
      return 5;
    case 'park-halloween':
      return 6;
    case 'park-christmas':
      return 7;
    case 'park-easter':
      return 8;
    case 'park-food':
      return 9;
    case 'park-services':
      return 10;
    default:
      return categoryKey.startsWith('item-') ? 20 : 99;
  }
}

function buildParkItemLink(park: Park, item: ParkItem, currentLanguage: string): string[] | null {
  return buildPublicParkItemRouteCommands({
    language: currentLanguage,
    parkId: park.id,
    parkName: park.name,
    itemId: item.id,
    itemName: item.name
  });
}

function resolveParkItemSourceIconClass(category: string | null | undefined): string {
  switch (category) {
    case 'Restaurant':
      return 'pi pi-utensils';
    case 'Hotel':
      return 'pi pi-building';
    case 'Show':
      return 'pi pi-ticket';
    case 'Shop':
      return 'pi pi-shopping-bag';
    case 'Service':
      return 'pi pi-info-circle';
    case 'Transport':
      return 'pi pi-send';
    case 'Animal':
      return 'pi pi-heart';
    case 'Attraction':
      return 'pi pi-bolt';
    default:
      return 'pi pi-star';
  }
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
