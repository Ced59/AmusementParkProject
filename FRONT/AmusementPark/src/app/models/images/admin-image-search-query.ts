import { ImageCategory } from './image-category';
import { ImageOwnerType } from './image-owner-type';

export type AdminImageSortField = 'created' | 'updated' | 'filename' | 'size' | 'dimensions';
export type AdminImageSortDirection = 'asc' | 'desc';

export interface AdminImageSearchQuery {
  page: number;
  size: number;
  search?: string | null;
  category?: ImageCategory | null;
  ownerType?: ImageOwnerType | null;
  ownerId?: string | null;
  tagId?: string | null;
  isPublished?: boolean | null;
  hasOwner?: boolean | null;
  hasGeoLocation?: boolean | null;
  sortBy?: AdminImageSortField | null;
  sortDirection?: AdminImageSortDirection | null;
}
