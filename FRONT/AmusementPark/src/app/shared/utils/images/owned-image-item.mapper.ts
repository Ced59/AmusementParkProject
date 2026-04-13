import { resolveLocalizedValue } from '@app/commons/localized-item.utils';
import { ImageDto } from '@app/models/images/image-dto';
import { OwnedImageItem } from '../../models/images/owned-image-item.model';

export function mapImageDtoToOwnedImageItem(image: ImageDto, languageCode: string = 'en'): OwnedImageItem {
  const resolvedAlt: string | undefined =
    resolveLocalizedValue<string>(image.altTexts, languageCode)
    ?? resolveLocalizedValue<string>(image.captions, languageCode);

  return {
    id: image.id,
    imageId: image.id,
    category: image.category,
    tagIds: image.tagIds ?? [],
    description: image.description ?? null,
    alt: resolvedAlt ?? image.description ?? image.originalFileName ?? image.id,
    isCurrent: image.isCurrent,
    createdAt: image.createdAt
  };
}
