import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { PARK_PHOTO_CATEGORIES, ParkPhotoCategoryDefinition } from '@app/models/images/park-photo-category';
import { Park } from '@app/models/parks/park';
import { buildPhotoCategories, buildPhotos } from './park-detail-gallery.mapper';

describe('park detail gallery mapper', () => {
  it('keeps every admin park photo category visible on public park galleries', () => {
    const tags: ImageTagDto[] = PARK_PHOTO_CATEGORIES.map((photoCategory: ParkPhotoCategoryDefinition, index: number) => {
      return createImageTag(`tag-${index}`, photoCategory.slug);
    });
    const photos: ImageDto[] = PARK_PHOTO_CATEGORIES.map((photoCategory: ParkPhotoCategoryDefinition, index: number) => {
      return createImage(`image-${index}`, [`tag-${index}`]);
    });

    const mappedPhotos = buildPhotos(createPark(), photos, [], tags, 'fr');
    const categories = buildPhotoCategories(mappedPhotos);

    expect(mappedPhotos.map((photo) => photo.categoryKey)).toEqual(PARK_PHOTO_CATEGORIES.map((photoCategory: ParkPhotoCategoryDefinition) => photoCategory.slug));
    expect(categories.map((category) => category.key)).toEqual(PARK_PHOTO_CATEGORIES.map((photoCategory: ParkPhotoCategoryDefinition) => photoCategory.slug));
    expect(categories.map((category) => category.labelKey)).toEqual(PARK_PHOTO_CATEGORIES.map((photoCategory: ParkPhotoCategoryDefinition) => photoCategory.publicLabelKey));
  });
});

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Phantasialand',
    countryCode: 'DE',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    descriptions: []
  };
}

function createImage(id: string, tagIds: string[]): ImageDto {
  return {
    id,
    category: ImageCategory.PARK,
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    path: `parks/${id}.jpg`,
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
