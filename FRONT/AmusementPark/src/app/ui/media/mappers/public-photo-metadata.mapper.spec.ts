import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { buildPublicPhotoMetadata } from './public-photo-metadata.mapper';

describe('public photo metadata mapper', () => {
  it('keeps localized descriptions and credits and falls back to the owner name without exposing the file name', () => {
    const photo: ImageDto = {
      id: 'photo-1',
      category: ImageCategory.PARK,
      ownerType: ImageOwnerType.PARK,
      isCurrent: false,
      isPublished: true,
      isWatermarked: false,
      width: 1200,
      height: 800,
      sizeInBytes: 1000,
      originalFileName: 'DSC_1234_PRIVATE.jpg',
      altTexts: [],
      captions: [{ languageCode: 'fr', value: 'Le château au coucher du soleil' }],
      credits: [{ languageCode: 'fr', value: 'Camille — Parc des merveilles' }],
      tagIds: [],
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z'
    };
    const options = {
      currentLanguage: 'fr',
      fallbackAlt: 'Parc des merveilles',
      fallbackTagKey: 'gallery',
      fallbackTagLabelKey: 'parks.photos.categories.gallery'
    };
    const localized = buildPublicPhotoMetadata(photo, new Map(), options);

    expect(localized.description).toBe('Le château au coucher du soleil');
    expect(localized.credit).toBe('Camille — Parc des merveilles');
    expect(localized.alt).toBe(localized.description);

    const withoutDescription = buildPublicPhotoMetadata({ ...photo, captions: [] }, new Map(), options);
    expect(withoutDescription.alt).toBe('Parc des merveilles');
    expect(withoutDescription.originalFileName).toBe(photo.originalFileName);
    expect(withoutDescription.width).toBe(1200);

    const explicitAlt = buildPublicPhotoMetadata({ ...photo, altTexts: [{ languageCode: 'fr', value: 'Le château illuminé' }] }, new Map(), options);
    expect(explicitAlt.alt).toBe('Le château illuminé');
  });
});
