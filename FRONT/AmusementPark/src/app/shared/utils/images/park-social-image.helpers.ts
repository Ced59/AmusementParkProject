import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';

export function resolveParkSummarySocialImageId(summary: ParkDetailSummary | null | undefined): string | null {
  return resolveParkPhotoSocialImageId(summary?.mainImage);
}

export function resolveParkPhotoSocialImageId(image: ImageDto | null | undefined): string | null {
  const imageId: string | null = normalizeOptionalImageId(image?.id);
  if (!image || image.category !== ImageCategory.PARK || image.isPublished === false || imageId === null) {
    return null;
  }

  return imageId;
}

export function resolveParkSocialImageId(parkPhotos: readonly ImageDto[]): string | null {
  const currentPhoto: ImageDto | undefined = parkPhotos.find((photo: ImageDto): boolean => {
    return photo.isCurrent && resolveParkPhotoSocialImageId(photo) !== null;
  });

  if (currentPhoto) {
    return resolveParkPhotoSocialImageId(currentPhoto);
  }

  const fallbackPhoto: ImageDto | undefined = parkPhotos.find((photo: ImageDto): boolean => {
    return resolveParkPhotoSocialImageId(photo) !== null;
  });

  return resolveParkPhotoSocialImageId(fallbackPhoto);
}

function normalizeOptionalImageId(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}
