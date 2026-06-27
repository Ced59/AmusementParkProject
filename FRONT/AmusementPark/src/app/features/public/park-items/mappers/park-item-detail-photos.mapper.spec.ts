import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { PARK_ITEM_PHOTO_CATEGORIES, ParkItemPhotoCategoryDefinition } from '@app/models/images/park-item-photo-category';
import { buildPhotoCategories, buildPhotos } from './park-item-detail-photos.mapper';

describe('park item detail photos mapper', () => {
  it('keeps every admin park item photo category visible on public item galleries', () => {
    const tags: ImageTagDto[] = PARK_ITEM_PHOTO_CATEGORIES.map((photoCategory: ParkItemPhotoCategoryDefinition, index: number) => {
      return createImageTag(`tag-${index}`, photoCategory.slug);
    });
    const photos: ImageDto[] = PARK_ITEM_PHOTO_CATEGORIES.map((photoCategory: ParkItemPhotoCategoryDefinition, index: number) => {
      return createImage(`image-${index}`, [`tag-${index}`]);
    });

    const mappedPhotos = buildPhotos(photos, tags, 'fr');
    const categories = buildPhotoCategories(mappedPhotos);

    expect(mappedPhotos.map((photo) => photo.categoryKey)).toEqual(PARK_ITEM_PHOTO_CATEGORIES.map((photoCategory: ParkItemPhotoCategoryDefinition) => photoCategory.publicKey));
    expect(categories.map((category) => category.key)).toEqual(PARK_ITEM_PHOTO_CATEGORIES.map((photoCategory: ParkItemPhotoCategoryDefinition) => photoCategory.publicKey));
    expect(categories.map((category) => category.labelKey)).toEqual(PARK_ITEM_PHOTO_CATEGORIES.map((photoCategory: ParkItemPhotoCategoryDefinition) => photoCategory.publicLabelKey));
  });
});

function createImage(id: string, tagIds: string[]): ImageDto {
  return {
    id,
    category: ImageCategory.PARK_ITEM,
    ownerType: ImageOwnerType.PARK_ITEM,
    ownerId: 'item-1',
    path: `items/${id}.jpg`,
    description: `Photo ${id}`,
    isCurrent: false,
    isWatermarked: false,
    isPublished: true,
    width: 1200,
    height: 800,
    sizeInBytes: 1000,
    originalFileName: `${id}.jpg`,
    contentType: 'image/jpeg',
    geoLocation: null,
    altTexts: [],
    captions: [],
    credits: [],
    tagIds,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function createImageTag(id: string, slug: string): ImageTagDto {
  return {
    id,
    slug,
    labels: [{ languageCode: 'fr', value: slug }],
    descriptions: [],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}
