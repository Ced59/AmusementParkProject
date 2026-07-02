import { resolveLocalizedText } from '@shared/utils/localization';
import { ImageDto } from '@app/models/images/image-dto';
import { OwnedImageItem } from '../../models/images/owned-image-item.model';

export function mapImageDtoToOwnedImageItem(image: ImageDto, languageCode: string = 'en'): OwnedImageItem {
  const resolvedAlt: string | null = normalizeOptionalString(resolveLocalizedText(image.altTexts, languageCode, ''));
  const resolvedCaption: string | null = normalizeOptionalString(resolveLocalizedText(image.captions, languageCode, ''));

  return {
    id: image.id,
    imageId: image.id,
    category: image.category,
    tagIds: image.tagIds ?? [],
    description: normalizeOptionalString(image.description),
    sourceUrl: image.sourceUrl ?? null,
    alt: resolvedAlt
      ?? resolvedCaption
      ?? normalizeOptionalString(image.description)
      ?? normalizeOptionalString(image.originalFileName)
      ?? image.id,
    isCurrent: image.isCurrent,
    createdAt: image.createdAt
  };
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}
