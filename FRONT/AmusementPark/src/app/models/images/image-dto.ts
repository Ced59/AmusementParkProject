import { ImageCategory } from './image-category';
import { ImageOwnerType } from './image-owner-type';

export interface ImageDto {
  id: string;
  category: ImageCategory;
  ownerType: ImageOwnerType;
  ownerId?: string;
  path?: string;
  description?: string;
  isCurrent: boolean;
  createdAt: string;
}
