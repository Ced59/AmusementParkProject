import { ImageCategory } from './image-category';
import { ImageOwnerType } from './image-owner-type';
import { ImageGeoLocation } from './image-geo-location';
import { LocalizedItemDto } from '../shared/localized-item-dto';

export interface ImageDto {
  id: string;
  category: ImageCategory;
  ownerType: ImageOwnerType;
  ownerId?: string;
  path?: string;
  description?: string;
  isCurrent: boolean;
  isPublished: boolean;
  width: number;
  height: number;
  sizeInBytes: number;
  originalFileName?: string;
  contentType?: string;
  geoLocation?: ImageGeoLocation | null;
  altTexts: LocalizedItemDto<string>[];
  captions: LocalizedItemDto<string>[];
  credits: LocalizedItemDto<string>[];
  tagIds: string[];
  createdAt: string;
  updatedAt: string;
}
