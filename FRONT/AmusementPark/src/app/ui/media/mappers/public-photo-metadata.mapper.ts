import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { UiPhotoCarouselImage, UiPhotoCarouselTagLabel } from '../models/ui-photo-carousel.model';

export interface PublicPhotoMetadataMappingOptions {
  currentLanguage: string;
  fallbackAlt: string;
  fallbackTagKey: string;
  fallbackTagLabelKey: string;
}

export type PublicPhotoMetadata = Pick<
  UiPhotoCarouselImage,
  | 'alt'
  | 'description'
  | 'caption'
  | 'credit'
  | 'takenOn'
  | 'uploadedAt'
  | 'updatedAt'
  | 'year'
  | 'yearLabel'
  | 'yearLabelKey'
  | 'tagKeys'
  | 'tagLabels'
  | 'latitude'
  | 'longitude'
  | 'width'
  | 'height'
  | 'sizeInBytes'
  | 'originalFileName'
  | 'contentType'
  | 'cameraMaker'
  | 'cameraModel'
  | 'focalLength'
  | 'aperture'
  | 'exposureTime'
  | 'iso'
  | 'orientation'
>;

export type PublicPhotoTagLookup = ReadonlyMap<string, UiPhotoCarouselTagLabel>;

const UnknownYearKey = 'unknown';
const UnknownYearLabelKey = 'ui.photoCarousel.years.unknown';

export function buildPublicPhotoTagLookup(
  imageTags: readonly ImageTagDto[],
  currentLanguage: string
): PublicPhotoTagLookup {
  const lookup: Map<string, UiPhotoCarouselTagLabel> = new Map<string, UiPhotoCarouselTagLabel>();

  for (const tag of imageTags) {
    if (tag.isActive === false) {
      continue;
    }

    const tagId: string | null = normalizeOptionalString(tag.id);
    if (!tagId) {
      continue;
    }

    const slug: string = normalizeOptionalString(tag.slug) ?? tagId;
    const label: string = normalizeOptionalString(resolveLocalizedValue(tag.labels, currentLanguage) ?? null) ?? slug;

    lookup.set(tagId, {
      key: slug,
      label,
      labelKey: null
    });
  }

  return lookup;
}

export function buildPublicPhotoMetadata(
  photo: ImageDto,
  tagLookup: PublicPhotoTagLookup,
  options: PublicPhotoMetadataMappingOptions
): PublicPhotoMetadata {
  const caption: string | null = normalizeOptionalString(
    resolveLocalizedValue(photo.captions, options.currentLanguage) ?? photo.description ?? null
  );
  const credit: string | null = normalizeOptionalString(resolveLocalizedValue(photo.credits, options.currentLanguage) ?? null);
  const alt: string = normalizeOptionalString(resolveLocalizedValue(photo.altTexts, options.currentLanguage) ?? null)
    ?? caption
    ?? normalizeOptionalString(photo.originalFileName)
    ?? options.fallbackAlt;
  const takenOn: string | null = normalizeOptionalString(photo.exifMetadata?.takenOnUtc);
  const year: string | null = resolveYear(takenOn);
  const tagLabels: UiPhotoCarouselTagLabel[] = resolvePhotoTags(photo, tagLookup, options);

  return {
    alt,
    description: caption,
    caption,
    credit,
    takenOn,
    uploadedAt: normalizeOptionalString(photo.createdAt),
    updatedAt: normalizeOptionalString(photo.updatedAt),
    year: year ?? UnknownYearKey,
    yearLabel: year ?? UnknownYearKey,
    yearLabelKey: year ? null : UnknownYearLabelKey,
    tagKeys: tagLabels.map((tag: UiPhotoCarouselTagLabel) => tag.key),
    tagLabels,
    latitude: normalizeFiniteNumber(photo.geoLocation?.latitude),
    longitude: normalizeFiniteNumber(photo.geoLocation?.longitude),
    width: normalizeFiniteNumber(photo.width),
    height: normalizeFiniteNumber(photo.height),
    sizeInBytes: normalizeFiniteNumber(photo.sizeInBytes),
    originalFileName: normalizeOptionalString(photo.originalFileName),
    contentType: normalizeOptionalString(photo.contentType),
    cameraMaker: normalizeOptionalString(photo.exifMetadata?.cameraMaker),
    cameraModel: normalizeOptionalString(photo.exifMetadata?.cameraModel),
    focalLength: normalizeFiniteNumber(photo.exifMetadata?.focalLength),
    aperture: normalizeFiniteNumber(photo.exifMetadata?.aperture),
    exposureTime: normalizeFiniteNumber(photo.exifMetadata?.exposureTime),
    iso: normalizeFiniteNumber(photo.exifMetadata?.iso),
    orientation: normalizeOptionalString(photo.exifMetadata?.orientation)
  };
}

function resolvePhotoTags(
  photo: ImageDto,
  tagLookup: PublicPhotoTagLookup,
  options: PublicPhotoMetadataMappingOptions
): UiPhotoCarouselTagLabel[] {
  const resolvedTags: UiPhotoCarouselTagLabel[] = [];
  const seenKeys: Set<string> = new Set<string>();

  for (const tagId of photo.tagIds ?? []) {
    const resolvedTag: UiPhotoCarouselTagLabel | undefined = tagLookup.get(tagId);
    if (!resolvedTag || seenKeys.has(resolvedTag.key)) {
      continue;
    }

    seenKeys.add(resolvedTag.key);
    resolvedTags.push(resolvedTag);
  }

  if (resolvedTags.length > 0) {
    return resolvedTags;
  }

  return [{
    key: options.fallbackTagKey,
    label: options.fallbackTagLabelKey,
    labelKey: options.fallbackTagLabelKey
  }];
}

function resolveYear(value: string | null): string | null {
  if (!value || value.length < 4) {
    return null;
  }

  const directYear: string = value.slice(0, 4);
  if (/^\d{4}$/.test(directYear)) {
    return directYear;
  }

  const parsedDate: Date = new Date(value);
  if (Number.isNaN(parsedDate.getTime())) {
    return null;
  }

  return String(parsedDate.getUTCFullYear());
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}

function normalizeFiniteNumber(value: number | null | undefined): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}
