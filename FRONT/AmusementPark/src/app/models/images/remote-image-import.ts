import { ImageCategory } from './image-category';
import { ImageOwnerType } from './image-owner-type';

export interface RemoteImageImport {
  sourceUrl: string;
  category: ImageCategory;
  ownerType: ImageOwnerType;
  ownerId?: string | null;
  description?: string | null;
  withWatermark: boolean;
  setAsCurrent: boolean;
}
