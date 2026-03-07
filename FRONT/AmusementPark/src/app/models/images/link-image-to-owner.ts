import { ImageOwnerType } from './image-owner-type';

export interface LinkImageToOwner {
  imageId: string;
  ownerType: ImageOwnerType;
  ownerId: string;
  description?: string;
  setAsCurrent: boolean;
}
