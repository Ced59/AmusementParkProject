import { ImageCategory } from '@app/models/images/image-category';

export interface OwnedImageItem {
  id: string;
  imageId: string;
  category: ImageCategory;
  tagIds: string[];
  description?: string | null;
  sourceUrl?: string | null;
  alt?: string | null;
  isCurrent: boolean;
  createdAt: string;
}
