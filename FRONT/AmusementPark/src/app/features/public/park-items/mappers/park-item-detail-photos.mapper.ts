import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { buildPublicPhotoMetadata, buildPublicPhotoTagLookup, PublicPhotoMetadata, PublicPhotoTagLookup } from '@ui/media';
import {
  ParkItemPhotoCategoryOptionViewModel,
  ParkItemPhotoViewModel
} from '../models/park-item-detail-view.model';
import { trimOrNull } from './park-item-detail-formatters';

export function buildPhotos(photos: ImageDto[], imageTags: ImageTagDto[], currentLanguage: string = 'en'): ParkItemPhotoViewModel[] {
  const tagLookup: PublicPhotoTagLookup = buildPublicPhotoTagLookup(imageTags, currentLanguage);

  return photos
    .filter((photo: ImageDto) => photo.isPublished !== false)
    .map((photo: ImageDto) => {
      const firstKnownTag: string | undefined = photo.tagIds
        ?.map((tagId: string) => tagLookup.get(tagId)?.key ?? tagId)
        .find((tagSlug: string) => !!tagSlug);
      const categoryKey: string = resolvePhotoCategoryKey(firstKnownTag);
      const categoryLabelKey: string = resolvePhotoCategoryLabelKey(categoryKey);
      const fallbackAlt: string = trimOrNull(photo.description) ?? trimOrNull(photo.originalFileName) ?? 'Park item photo';
      const metadata: PublicPhotoMetadata = buildPublicPhotoMetadata(photo, tagLookup, {
        currentLanguage,
        fallbackAlt,
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
        isCurrent: photo.isCurrent
      };
    });
}

export function buildPhotoCategories(photos: ParkItemPhotoViewModel[]): ParkItemPhotoCategoryOptionViewModel[] {
  const countByCategoryKey: Map<string, number> = new Map<string, number>();

  for (const photo of photos) {
    countByCategoryKey.set(photo.categoryKey, (countByCategoryKey.get(photo.categoryKey) ?? 0) + 1);
  }

  return Array.from(countByCategoryKey.entries())
    .map(([key, count]: [string, number]) => ({
      key,
      labelKey: resolvePhotoCategoryLabelKey(key),
      count
    }))
    .sort((first: ParkItemPhotoCategoryOptionViewModel, second: ParkItemPhotoCategoryOptionViewModel) => {
      return getPhotoCategoryOrder(first.key) - getPhotoCategoryOrder(second.key);
    });
}

function resolvePhotoCategoryKey(tagIdOrSlug: string | undefined): string {
  const normalizedValue: string = (tagIdOrSlug ?? '').toLowerCase();

  if (normalizedValue.includes('entrance')) {
    return 'entrance';
  }

  if (normalizedValue.includes('exit')) {
    return 'exit';
  }

  if (normalizedValue.includes('layout')) {
    return 'layout';
  }

  if (normalizedValue.includes('queue')) {
    return 'queue';
  }

  if (normalizedValue.includes('station')) {
    return 'station';
  }

  return 'gallery';
}

function resolvePhotoCategoryLabelKey(categoryKey: string): string {
  return `parkItems.photos.categories.${categoryKey}`;
}

function getPhotoCategoryOrder(categoryKey: string): number {
  switch (categoryKey) {
    case 'gallery':
      return 0;
    case 'entrance':
      return 1;
    case 'exit':
      return 2;
    case 'queue':
      return 3;
    case 'station':
      return 4;
    case 'layout':
      return 5;
    default:
      return 99;
  }
}
