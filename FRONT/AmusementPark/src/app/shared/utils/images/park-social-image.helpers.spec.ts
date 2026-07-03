import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { resolveParkPhotoSocialImageId, resolveParkSocialImageId, resolveParkSummarySocialImageId } from './park-social-image.helpers';

describe('park social image helpers', () => {
  it('keeps only published park photos as social images', () => {
    expect(resolveParkPhotoSocialImageId(createImage('logo-1', ImageCategory.LOGO))).toBeNull();
    expect(resolveParkPhotoSocialImageId(createImage('photo-1', ImageCategory.PARK))).toBe('photo-1');
    expect(resolveParkPhotoSocialImageId(createImage('hidden-photo', ImageCategory.PARK, false))).toBeNull();
    expect(resolveParkPhotoSocialImageId(createImage('   ', ImageCategory.PARK))).toBeNull();
  });

  it('prefers the current published park photo from galleries', () => {
    expect(resolveParkSocialImageId([
      createImage('photo-1', ImageCategory.PARK),
      createImage('current-photo', ImageCategory.PARK, true, true),
      createImage('logo-1', ImageCategory.LOGO, true, true)
    ])).toBe('current-photo');
  });

  it('filters logo fallback images from park summaries', () => {
    expect(resolveParkSummarySocialImageId(createSummary(createImage('logo-1', ImageCategory.LOGO)))).toBeNull();
    expect(resolveParkSummarySocialImageId(createSummary(createImage('photo-1', ImageCategory.PARK)))).toBe('photo-1');
  });
});

function createSummary(mainImage: ImageDto): ParkDetailSummary {
  return {
    park: {
      id: 'park-1',
      name: 'Park',
      countryCode: 'FR',
      latitude: 48.85,
      longitude: 2.35,
      isVisible: true,
      descriptions: []
    },
    mainImage,
    references: {},
    stats: {
      totalItems: 0,
      zoneCount: 0,
      attractionCount: 0,
      restaurantCount: 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {}
    }
  };
}

function createImage(id: string, category: ImageCategory, isPublished: boolean = true, isCurrent: boolean = false): ImageDto {
  return {
    id,
    category,
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    isCurrent,
    isPublished,
    isWatermarked: false,
    width: 1200,
    height: 630,
    sizeInBytes: 1,
    contentType: 'image/jpeg',
    sourceUrl: null,
    geoLocation: null,
    exifMetadata: null,
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: [],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}
