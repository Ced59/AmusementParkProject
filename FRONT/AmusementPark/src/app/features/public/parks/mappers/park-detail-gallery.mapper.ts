import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { normalizeTranslationSegment } from '@shared/utils/display/display-label.helpers';
import { getParkItemCategoryTranslationKey } from '@shared/utils/display/park-item-presentation.helpers';
import { buildPublicParkItemRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { buildPublicPhotoMetadata, buildPublicPhotoTagLookup, PublicPhotoMetadata, PublicPhotoTagLookup } from '@ui/media';
import { ParkDetailPhotoCategoryOptionViewModel, ParkDetailPhotoViewModel } from '../models/park-detail-view.model';
import { ParkDetailItemPhotoSource } from './park-detail-mapping.model';
import { normalizeOptionalString } from './park-detail-info.mapper';

export function resolveParkHeroImageId(parkPhotos: ImageDto[]): string | null {

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

export function buildPhotos(
  park: Park,
  parkPhotos: ImageDto[],
  itemPhotoSources: ParkDetailItemPhotoSource[],
  imageTags: ImageTagDto[],
  currentLanguage: string
): ParkDetailPhotoViewModel[] {
  const tagLookup: PublicPhotoTagLookup = buildPublicPhotoTagLookup(imageTags, currentLanguage);
  const mappedParkPhotos: ParkDetailPhotoViewModel[] = buildParkPhotos(park, parkPhotos, tagLookup, currentLanguage);
  const mappedItemPhotos: ParkDetailPhotoViewModel[] = itemPhotoSources.flatMap((source: ParkDetailItemPhotoSource) => {
    return buildItemPhotos(park, source.item, source.photos, tagLookup, currentLanguage);
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

function buildParkPhotos(
  park: Park,
  photos: ImageDto[],
  tagLookup: PublicPhotoTagLookup,
  currentLanguage: string
): ParkDetailPhotoViewModel[] {
  const displayablePhotos: ImageDto[] = photos.filter((photo: ImageDto) => isDisplayableParkPhoto(photo));

  return displayablePhotos
    .map((photo: ImageDto) => {
      const firstKnownTag: string | undefined = photo.tagIds
        ?.map((tagId: string) => tagLookup.get(tagId)?.key ?? tagId)
        .find((tagSlug: string) => !!tagSlug);
      const categoryKey: string = resolveParkPhotoCategoryKey(firstKnownTag);
      const categoryLabelKey: string = resolveParkPhotoCategoryLabelKey(categoryKey);
      const parkName: string = park.name?.trim() ?? 'Park';
      const metadata: PublicPhotoMetadata = buildPublicPhotoMetadata(photo, tagLookup, {
        currentLanguage,
        fallbackAlt: `${parkName} photo`,
        fallbackTagKey: categoryKey,
        fallbackTagLabelKey: categoryLabelKey
      });

      return {
        id: photo.id,
        imageId: photo.id,
        category: photo.category,
        categoryKey,
        categoryLabelKey,
        ...metadata,
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
  tagLookup: PublicPhotoTagLookup,
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
      const metadata: PublicPhotoMetadata = buildPublicPhotoMetadata(photo, tagLookup, {
        currentLanguage,
        fallbackAlt: `${itemName} photo`,
        fallbackTagKey: categoryKey,
        fallbackTagLabelKey: categoryLabelKey
      });

      return {
        id: photo.id,
        imageId: photo.id,
        category: photo.category,
        categoryKey,
        categoryLabelKey,
        ...metadata,
        isCurrent: photo.isCurrent,
        sourceTitle: itemName,
        sourceSubtitle: categoryLabelKey,
        sourceIconClass: resolveParkItemSourceIconClass(item.category),
        sourceRouterLink: itemLink,
        sourceLinkLabelKey: 'parks.photos.openItem'
      };
    });
}

export function buildPhotoCategories(photos: ParkDetailPhotoViewModel[]): ParkDetailPhotoCategoryOptionViewModel[] {
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
    || normalizedCategory === ImageCategory.LOGO
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
