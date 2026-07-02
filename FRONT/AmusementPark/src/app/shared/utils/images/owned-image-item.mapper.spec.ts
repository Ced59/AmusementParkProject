import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { mapImageDtoToOwnedImageItem } from './owned-image-item.mapper';

describe('mapImageDtoToOwnedImageItem', () => {
  it('falls back to caption, description, file name and id when alt text is blank', () => {
    expect(mapImageDtoToOwnedImageItem(createImage({ altTexts: [{ languageCode: 'en', value: '   ' }], captions: [{ languageCode: 'en', value: 'Night view' }] })).alt)
      .toBe('Night view');
    expect(mapImageDtoToOwnedImageItem(createImage({ description: 'Main entrance' })).alt)
      .toBe('Main entrance');
    expect(mapImageDtoToOwnedImageItem(createImage({ originalFileName: 'coaster.jpg' })).alt)
      .toBe('coaster.jpg');
    expect(mapImageDtoToOwnedImageItem(createImage({ id: 'image-id' })).alt)
      .toBe('image-id');
  });
});

function createImage(partial: Partial<ImageDto>): ImageDto {
  return {
    id: 'image-1',
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    category: ImageCategory.PARK,
    isCurrent: false,
    isPublished: true,
    isWatermarked: false,
    width: 100,
    height: 100,
    sizeInBytes: 1000,
    sourceUrl: null,
    geoLocation: null,
    exifMetadata: null,
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: [],
    createdAt: '2026-07-02T00:00:00Z',
    updatedAt: '2026-07-02T00:00:00Z',
    ...partial
  };
}
