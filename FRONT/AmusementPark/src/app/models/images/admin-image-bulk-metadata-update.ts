import { ImageCategory } from './image-category';

export interface AdminImageBulkMetadataUpdate {
  imageIds: string[];
  isPublished?: boolean | null;
  category?: ImageCategory | null;
  addTagIds?: string[];
  removeTagIds?: string[];
}

export interface AdminImageBulkMetadataResult {
  requestedCount: number;
  updatedCount: number;
}
